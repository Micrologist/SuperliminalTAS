using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SuperliminalTAS.Demo;

/// <summary>
/// CSV format read/write for editing TAS inputs in Excel or other spreadsheet programs.
/// Each row represents one frame of input.
/// </summary>
public static class DemoCSVSerializer
{
    public static string Serialize(DemoData data)
    {
        if (data == null || data.FrameCount < 1) return null;

        var sb = new StringBuilder();

        // Metadata rows
        if (!string.IsNullOrEmpty(data.LevelId))
            sb.AppendLine($"Level: {data.LevelId}");
        if (data.CheckpointId >= 0)
            sb.AppendLine($"Checkpoint: {data.CheckpointId}");

        // Header row
        foreach (var axis in DemoActions.Axes)
            sb.Append($"{axis},");
        foreach (var button in DemoActions.Buttons)
            sb.Append($"{button},");
        sb.Append("Reset Checkpoint,");
        sb.Append("Speed");
        sb.AppendLine();

        // Data rows (one per frame)
        for (int frame = 0; frame < data.FrameCount; frame++)
        {
            // Axes
            foreach (var axis in DemoActions.Axes)
            {
                float value = data.GetAxis(axis, frame);
                sb.Append(value.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(',');
            }

            // Buttons (current state)
            foreach (var button in DemoActions.Buttons)
            {
                bool value = data.GetButton(button, frame);
                sb.Append(value ? "1," : "0,");
            }

            // Reset Checkpoint flag
            bool resetCheckpoint = data.GetCheckpointReset(frame);
            sb.Append(resetCheckpoint ? "1," : "0,");

            // Speed (optional per-frame playback speed multiplier)
            var speed = data.GetSpeed(frame);
            if (speed.HasValue)
                sb.Append($"{speed.Value}x");

            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static DemoData Deserialize(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
            throw new InvalidDataException("CSV content is empty.");

        var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
            throw new InvalidDataException("CSV must have at least a header row and one data row.");

        // Parse metadata rows
        string levelId = "";
        int checkpointId = -1;
        int currentLine = 0;

        // Check for Level metadata
        if (lines[currentLine].StartsWith("Level:", StringComparison.OrdinalIgnoreCase))
        {
            // Extract level name, stopping at first comma if present (in case of malformed CSV)
            var levelLine = lines[currentLine].Substring("Level:".Length).Trim();
            var commaIndex = levelLine.IndexOf(',');
            levelId = commaIndex >= 0 ? levelLine.Substring(0, commaIndex).Trim() : levelLine;
            currentLine++;
        }

        // Check for Checkpoint metadata
        if (currentLine < lines.Length && lines[currentLine].StartsWith("Checkpoint:", StringComparison.OrdinalIgnoreCase))
        {
            // Extract checkpoint ID, stopping at first comma if present
            var checkpointLine = lines[currentLine].Substring("Checkpoint:".Length).Trim();
            var commaIndex = checkpointLine.IndexOf(',');
            var checkpointStr = commaIndex >= 0 ? checkpointLine.Substring(0, commaIndex).Trim() : checkpointLine;
            if (int.TryParse(checkpointStr, out int parsed))
                checkpointId = parsed;
            currentLine++;
        }

        if (currentLine >= lines.Length - 1)
            throw new InvalidDataException("CSV must have at least a header row and one data row after metadata.");

        // Parse header to validate structure
        var header = lines[currentLine].Split(',');
        int requiredColumns = DemoActions.Axes.Length + DemoActions.Buttons.Length;

        // Check if we have the new format with Reset Checkpoint column
        bool hasResetCheckpointColumn = false;
        for (int i = 0; i < header.Length; i++)
        {
            if (header[i].Trim().Equals("Reset Checkpoint", StringComparison.OrdinalIgnoreCase))
            {
                hasResetCheckpointColumn = true;
                break;
            }
        }

        int minRequiredColumns = requiredColumns + (hasResetCheckpointColumn ? 1 : 0);
        bool hasSpeedColumn = header.Length > minRequiredColumns;

        if (header.Length < requiredColumns)
            throw new InvalidDataException($"Expected at least {requiredColumns} columns, got {header.Length}.");

        int frameCount = lines.Length - currentLine - 1; // Exclude header and metadata

        var axes = new Dictionary<string, List<float>>(DemoActions.Axes.Length);
        foreach (var axis in DemoActions.Axes)
            axes[axis] = new List<float>(frameCount);

        var buttons = new Dictionary<string, List<bool>>(DemoActions.Buttons.Length);
        foreach (var button in DemoActions.Buttons)
            buttons[button] = new List<bool>(frameCount);

        var speeds = hasSpeedColumn ? new List<float?>(frameCount) : null;
        var checkpointResets = hasResetCheckpointColumn ? new List<bool>(frameCount) : null;

        // Parse data rows
        for (int i = currentLine + 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(',');
            // Allow extra columns for notes/metadata, just check we have minimum required
            int minimumColumns = minRequiredColumns;
            if (hasSpeedColumn) minimumColumns++;

            if (values.Length < minimumColumns)
                throw new InvalidDataException($"Row {i} has {values.Length} columns, expected at least {minimumColumns}.");

            int col = 0;

            // Parse axes
            foreach (var axis in DemoActions.Axes)
            {
                if (!float.TryParse(values[col], NumberStyles.Float, CultureInfo.InvariantCulture, out float axisValue))
                    throw new InvalidDataException($"Invalid float value at row {i}, column {col}: '{values[col]}'");
                axes[axis].Add(axisValue);
                col++;
            }

            // Parse buttons (current state)
            foreach (var button in DemoActions.Buttons)
            {
                bool buttonValue = ParseBool(values[col], i, col);
                buttons[button].Add(buttonValue);
                col++;
            }

            // Parse checkpoint reset (if column exists)
            if (hasResetCheckpointColumn)
            {
                bool resetValue = ParseBool(values[col], i, col);
                checkpointResets.Add(resetValue);
                col++;
            }

            // Parse speed (if column exists)
            if (hasSpeedColumn)
            {
                speeds.Add(ParseSpeed(values[col], i, col));
            }
        }

        var data = DemoData.CreateEmpty();
        data.ReplaceAll(axes, buttons, speeds, checkpointResets, levelId, checkpointId);
        return data;
    }

    private static bool ParseBool(string value, int row, int col)
    {
        if (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (value == "0" || value.Equals("false", StringComparison.OrdinalIgnoreCase))
            return false;

        throw new InvalidDataException($"Invalid boolean value at row {row}, column {col}: '{value}' (expected 0/1 or true/false)");
    }

    private static float? ParseSpeed(string value, int row, int col)
    {
        // Empty or whitespace means no speed change
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Remove 'x' suffix if present
        var speedStr = value.Trim();
        if (speedStr.EndsWith("x", StringComparison.OrdinalIgnoreCase))
            speedStr = speedStr.Substring(0, speedStr.Length - 1);

        if (float.TryParse(speedStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float speedValue))
        {
            if (speedValue > 0)
                return speedValue;
            throw new InvalidDataException($"Speed multiplier at row {row}, column {col} must be positive: '{value}'");
        }

        throw new InvalidDataException($"Invalid speed value at row {row}, column {col}: '{value}' (expected format: '8x', '0.02x', or empty)");
    }
}