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

        // Header row
        foreach (var axis in DemoActions.Axes)
            sb.Append($"{axis},");
        foreach (var button in DemoActions.Buttons)
            sb.Append($"{button},");
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

        // Parse header to validate structure
        var header = lines[0].Split(',');
        int expectedColumns = DemoActions.Axes.Length + DemoActions.Buttons.Length;
        bool hasSpeedColumn = header.Length == expectedColumns + 1;

        if (header.Length != expectedColumns && !hasSpeedColumn)
            throw new InvalidDataException($"Expected {expectedColumns} or {expectedColumns + 1} columns, got {header.Length}.");

        int frameCount = lines.Length - 1; // Exclude header

        var axes = new Dictionary<string, List<float>>(DemoActions.Axes.Length);
        foreach (var axis in DemoActions.Axes)
            axes[axis] = new List<float>(frameCount);

        var buttons = new Dictionary<string, List<bool>>(DemoActions.Buttons.Length);
        foreach (var button in DemoActions.Buttons)
            buttons[button] = new List<bool>(frameCount);

        var speeds = hasSpeedColumn ? new List<float?>(frameCount) : null;

        // Parse data rows
        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(',');
            int expectedRowColumns = hasSpeedColumn ? expectedColumns + 1 : expectedColumns;
            if (values.Length != expectedRowColumns)
                throw new InvalidDataException($"Row {i} has {values.Length} columns, expected {expectedRowColumns}.");

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

            // Parse speed (if column exists)
            if (hasSpeedColumn)
            {
                speeds.Add(ParseSpeed(values[col], i, col));
            }
        }

        var data = DemoData.CreateEmpty();
        data.ReplaceAll(axes, buttons, speeds);
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
