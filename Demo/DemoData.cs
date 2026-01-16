using Rewired;
using System;
using System.Collections.Generic;

namespace SuperliminalTAS.Demo;

internal static class DemoActions
{
    public static readonly string[] Buttons = { "Jump", "Grab", "Rotate" };
    public static readonly string[] Axes =
    [
        "Move Horizontal",
        "Move Vertical",
        "Look Horizontal",
        "Look Vertical"
    ];
}

[Serializable]
public sealed class DemoData
{
    private readonly Dictionary<string, List<bool>> _button = [];
    private readonly Dictionary<string, List<bool>> _buttonDown = [];
    private readonly Dictionary<string, List<bool>> _buttonUp = [];
    private readonly Dictionary<string, List<float>> _axis = [];

    public int FrameCount => _button.TryGetValue("Jump", out var list) ? list.Count : 0;

    private DemoData() { }

    public static DemoData CreateEmpty()
    {
        var d = new DemoData();

        foreach (var b in DemoActions.Buttons)
        {
            d._button[b] = new List<bool>(1024);
            d._buttonDown[b] = new List<bool>(1024);
            d._buttonUp[b] = new List<bool>(1024);
        }

        foreach (var a in DemoActions.Axes)
            d._axis[a] = new List<float>(1024);

        return d;
    }

    public void RecordFrameFrom(Player input)
    {
        foreach (var b in DemoActions.Buttons)
        {
            _button[b].Add(input.GetButton(b));
            _buttonDown[b].Add(input.GetButtonDown(b));
            _buttonUp[b].Add(input.GetButtonUp(b));
        }

        foreach (var a in DemoActions.Axes)
            _axis[a].Add(input.GetAxis(a));
    }

    public bool GetButton(string actionName, int frame) => _button[actionName][frame];
    public bool GetButtonDown(string actionName, int frame) => _buttonDown[actionName][frame];
    public bool GetButtonUp(string actionName, int frame) => _buttonUp[actionName][frame];
    public float GetAxis(string actionName, int frame) => _axis[actionName][frame];

    internal Dictionary<string, List<bool>> Buttons => _button;
    internal Dictionary<string, List<bool>> ButtonsDown => _buttonDown;
    internal Dictionary<string, List<bool>> ButtonsUp => _buttonUp;
    internal Dictionary<string, List<float>> Axes => _axis;

    internal void ReplaceAll(
        Dictionary<string, List<float>> axes,
        Dictionary<string, List<bool>> buttons,
        Dictionary<string, List<bool>> buttonsDown,
        Dictionary<string, List<bool>> buttonsUp)
    {
        _axis.Clear();
        _button.Clear();
        _buttonDown.Clear();
        _buttonUp.Clear();

        foreach (var kv in axes) _axis[kv.Key] = kv.Value;
        foreach (var kv in buttons) _button[kv.Key] = kv.Value;
        foreach (var kv in buttonsDown) _buttonDown[kv.Key] = kv.Value;
        foreach (var kv in buttonsUp) _buttonUp[kv.Key] = kv.Value;
    }
}