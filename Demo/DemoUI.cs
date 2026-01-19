using SFB;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SuperliminalTAS.Demo;

public sealed class DemoFileDialog
{
    private readonly StandaloneFileBrowserWindows _fileBrowser = new();

    private static readonly ExtensionFilter[] OpenExtensionList =
    {
        new("CSV File (*.csv)", "csv"),
        new("All Files", "*")
    };

    private static readonly ExtensionFilter[] SaveExtensionList =
    {
        new("CSV File (*.csv)", "csv"),
        new("All Files", "*")
    };

    public string DemoDirectory { get; }

    public DemoFileDialog()
    {
        DemoDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "demos");
        if (!Directory.Exists(DemoDirectory))
            Directory.CreateDirectory(DemoDirectory);
    }

    public string OpenPath()
    {
        var selected = _fileBrowser.OpenFilePanel("Open TAS File", DemoDirectory, OpenExtensionList, false);
        return selected.FirstOrDefault()?.Name;
    }

    public string SavePath()
    {
        var name = $"SuperliminalTAS-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.csv";
        var selected = _fileBrowser.SaveFilePanel("Save Recording as", DemoDirectory, name, SaveExtensionList);
        return selected?.Name;
    }
}

internal static class DemoStatusText
{
    public static Text CreateStatusText(
        Transform parentCanvas,
        string fontName,
        int fontSize,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        var root = new GameObject("TASMod_UI");
        root.transform.SetParent(parentCanvas, worldPositionStays: false);
        root.AddComponent<CanvasGroup>().blocksRaycasts = false;

        var text = root.AddComponent<Text>();
        text.fontSize = fontSize;

        foreach (var font in Resources.FindObjectsOfTypeAll<Font>())
        {
            if (font != null && font.name == fontName)
            {
                text.font = font;
                break;
            }
        }

        var rect = text.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.pivot = new Vector2(0f, 1f);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;

        return text;
    }
}
