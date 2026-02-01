using Rewired;
using System;
using System.Collections.Generic;

namespace SuperliminalTools.TASMod.Demo;

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
    private readonly List<float?> _speed = [];
    private readonly List<bool> _checkpointReset = [];

    // Metadata for level and checkpoint tracking
    public string LevelId { get; set; } = "";
    public int CheckpointId { get; set; } = -1; // -1 means start of level

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

    public float? GetSpeed(int frame) => frame < _speed.Count ? _speed[frame] : null;

    public bool GetCheckpointReset(int frame) => frame < _checkpointReset.Count && _checkpointReset[frame];

    public void SetCheckpointReset(int frame, bool value)
    {
        // Ensure the list is large enough
        while (_checkpointReset.Count <= frame)
            _checkpointReset.Add(false);

        _checkpointReset[frame] = value;
    }

    internal Dictionary<string, List<bool>> Buttons => _button;
    internal Dictionary<string, List<float>> Axes => _axis;
    internal List<float?> Speeds => _speed;
    internal List<bool> CheckpointResets => _checkpointReset;

    internal void ReplaceAll(
        Dictionary<string, List<float>> axes,
        Dictionary<string, List<bool>> buttons,
        List<float?> speeds = null,
        List<bool> checkpointResets = null,
        string levelId = "",
        int checkpointId = -1)
    {
        _axis.Clear();
        _button.Clear();
        _speed.Clear();
        _checkpointReset.Clear();

        foreach (var kv in axes) _axis[kv.Key] = kv.Value;
        foreach (var kv in buttons) _button[kv.Key] = kv.Value;
        if (speeds != null)
        {
            foreach (var s in speeds) _speed.Add(s);
        }
        if (checkpointResets != null)
        {
            foreach (var r in checkpointResets) _checkpointReset.Add(r);
        }

        LevelId = levelId;
        CheckpointId = checkpointId;
    }
}