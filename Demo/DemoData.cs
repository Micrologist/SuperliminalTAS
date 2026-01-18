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
    private readonly Dictionary<string, List<float>> _axis = [];

    public int FrameCount => _button.TryGetValue("Jump", out var list) ? list.Count : 0;

    private DemoData() { }

    public static DemoData CreateEmpty()
    {
        var d = new DemoData();

        foreach (var b in DemoActions.Buttons)
            d._button[b] = new List<bool>(1024);

        foreach (var a in DemoActions.Axes)
            d._axis[a] = new List<float>(1024);

        return d;
    }

    public void RecordFrameFrom(Player input)
    {
        foreach (var b in DemoActions.Buttons)
            _button[b].Add(input.GetButton(b));

        foreach (var a in DemoActions.Axes)
            _axis[a].Add(input.GetAxis(a));
    }

    public bool GetButton(string actionName, int frame) => _button[actionName][frame];

    public bool GetButtonDown(string actionName, int frame)
    {
        var buttonList = _button[actionName];
        return buttonList[frame] && (frame == 0 || !buttonList[frame - 1]);
    }

    public bool GetButtonUp(string actionName, int frame)
    {
        var buttonList = _button[actionName];
        return !buttonList[frame] && frame > 0 && buttonList[frame - 1];
    }

    public float GetAxis(string actionName, int frame) => _axis[actionName][frame];

    internal Dictionary<string, List<bool>> Buttons => _button;
    internal Dictionary<string, List<float>> Axes => _axis;

    internal void ReplaceAll(
        Dictionary<string, List<float>> axes,
        Dictionary<string, List<bool>> buttons)
    {
        _axis.Clear();
        _button.Clear();

        foreach (var kv in axes) _axis[kv.Key] = kv.Value;
        foreach (var kv in buttons) _button[kv.Key] = kv.Value;
    }
}