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
        sb.Append("Frame,");
        foreach (var axis in DemoActions.Axes)
            sb.Append($"{axis},");
        foreach (var button in DemoActions.Buttons)
            sb.Append($"{button},");
        foreach (var button in DemoActions.Buttons)
            sb.Append($"{button}Down,");
        foreach (var button in DemoActions.Buttons)
            sb.Append($"{button}Up,");
        sb.Length--; // Remove trailing comma
        sb.AppendLine();

        // Data rows (one per frame)
        for (int frame = 0; frame < data.FrameCount; frame++)
        {
            sb.Append($"{frame},");

            // Axes
            foreach (var axis in DemoActions.Axes)
            {
                float value = data.GetAxis(axis, frame);
                sb.Append(value.ToString("F6", CultureInfo.InvariantCulture));
                sb.Append(',');
            }

            // Buttons (current state)
            foreach (var button in DemoActions.Buttons)
            {
                bool value = data.GetButton(button, frame);
                sb.Append(value ? "1," : "0,");
            }

            // ButtonsDown
            foreach (var button in DemoActions.Buttons)
            {
                bool value = data.GetButtonDown(button, frame);
                sb.Append(value ? "1," : "0,");
            }

            // ButtonsUp
            foreach (var button in DemoActions.Buttons)
            {
                bool value = data.GetButtonUp(button, frame);
                sb.Append(value ? "1," : "0,");
            }

            sb.Length--; // Remove trailing comma
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
        int expectedColumns = 1 + DemoActions.Axes.Length + (DemoActions.Buttons.Length * 3);
        if (header.Length != expectedColumns)
            throw new InvalidDataException($"Expected {expectedColumns} columns, got {header.Length}.");

        int frameCount = lines.Length - 1; // Exclude header

        var axes = new Dictionary<string, List<float>>(DemoActions.Axes.Length);
        foreach (var axis in DemoActions.Axes)
            axes[axis] = new List<float>(frameCount);

        var buttons = new Dictionary<string, List<bool>>(DemoActions.Buttons.Length);
        var buttonsDown = new Dictionary<string, List<bool>>(DemoActions.Buttons.Length);
        var buttonsUp = new Dictionary<string, List<bool>>(DemoActions.Buttons.Length);
        foreach (var button in DemoActions.Buttons)
        {
            buttons[button] = new List<bool>(frameCount);
            buttonsDown[button] = new List<bool>(frameCount);
            buttonsUp[button] = new List<bool>(frameCount);
        }

        // Parse data rows
        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(',');
            if (values.Length != expectedColumns)
                throw new InvalidDataException($"Row {i} has {values.Length} columns, expected {expectedColumns}.");

            int col = 1; // Skip frame number column

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

            // Parse buttonsDown
            foreach (var button in DemoActions.Buttons)
            {
                bool buttonValue = ParseBool(values[col], i, col);
                buttonsDown[button].Add(buttonValue);
                col++;
            }

            // Parse buttonsUp
            foreach (var button in DemoActions.Buttons)
            {
                bool buttonValue = ParseBool(values[col], i, col);
                buttonsUp[button].Add(buttonValue);
                col++;
            }
        }

        var data = DemoData.CreateEmpty();
        data.ReplaceAll(axes, buttons, buttonsDown, buttonsUp);
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
}
