using SFB;
using System;
using System.IO;
using System.Linq;

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
