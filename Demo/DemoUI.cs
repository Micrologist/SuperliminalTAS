using System;
using System.IO;
using System.Linq;

#if LEGACY
using System.Runtime.InteropServices;
using System.Text;

// Minimal ExtensionFilter for Win32 file dialogs
public readonly struct ExtensionFilter
{
    public readonly string Name;
    public readonly string[] Extensions;

    public ExtensionFilter(string name, params string[] extensions)
    {
        Name = name;
        Extensions = extensions ?? Array.Empty<string>();
    }
}
#else
using SFB;
#endif

public sealed class DemoFileDialog
{
#if !LEGACY
    private readonly StandaloneFileBrowserWindows _fileBrowser = new();
#endif

    private static readonly ExtensionFilter[] OpenExtensionList =
    {
#if LEGACY
        new("CSV File (*.csv)", "csv"),
        new("All Files (*.*)", "*")
#else
        new("CSV File (*.csv)", "csv"),
        new("All Files", "*")
#endif
    };

    private static readonly ExtensionFilter[] SaveExtensionList =
    {
#if LEGACY
        new("CSV File (*.csv)", "csv"),
        new("All Files (*.*)", "*")
#else
        new("CSV File (*.csv)", "csv"),
        new("All Files", "*")
#endif
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
#if LEGACY
        return Win32FileDialogs.OpenFile(
            title: "Open TAS File",
            initialDirectory: DemoDirectory,
            filters: OpenExtensionList,
            defaultExtension: "csv"
        );
#else
        var selected = _fileBrowser.OpenFilePanel("Open TAS File", DemoDirectory, OpenExtensionList, false);
        return selected.FirstOrDefault()?.Name;
#endif
    }

    public string SavePath()
    {
        var name = $"SuperliminalTAS-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.csv";
#if LEGACY
        return Win32FileDialogs.SaveFile(
            title: "Save Recording as",
            initialDirectory: DemoDirectory,
            defaultFileName: name,
            filters: SaveExtensionList,
            defaultExtension: "csv"
        );
#else
        var selected = _fileBrowser.SaveFilePanel("Save Recording as", DemoDirectory, name, SaveExtensionList);
        return selected?.Name;
#endif
    }
}

#if LEGACY
internal static class Win32FileDialogs
{
    // --- Public API ---
    public static string OpenFile(string title, string initialDirectory, ExtensionFilter[] filters, string defaultExtension)
    {
        var ofn = CreateBaseOFN(title, initialDirectory, filters, defaultExtension);

        // Allow long paths; Win32 API uses buffer you provide.
        var fileBuffer = new string('\0', 32768);
        ofn.lpstrFile = fileBuffer;
        ofn.nMaxFile = fileBuffer.Length;

        ofn.Flags =
            OFN_EXPLORER |
            OFN_PATHMUSTEXIST |
            OFN_FILEMUSTEXIST |
            OFN_NOCHANGEDIR |
            OFN_HIDEREADONLY;

        if (!GetOpenFileNameW(ref ofn))
            return null;

        // lpstrFile comes back as a string with possible trailing nulls.
        return TrimAtFirstNull(ofn.lpstrFile);
    }

    public static string SaveFile(string title, string initialDirectory, string defaultFileName, ExtensionFilter[] filters, string defaultExtension)
    {
        var ofn = CreateBaseOFN(title, initialDirectory, filters, defaultExtension);

        var fileBuffer = new string('\0', 32768);
        // Pre-fill with default file name.
        fileBuffer = WriteIntoNullTerminatedBuffer(fileBuffer, defaultFileName);

        ofn.lpstrFile = fileBuffer;
        ofn.nMaxFile = fileBuffer.Length;

        ofn.Flags =
            OFN_EXPLORER |
            OFN_PATHMUSTEXIST |
            OFN_OVERWRITEPROMPT |
            OFN_NOCHANGEDIR |
            OFN_HIDEREADONLY;

        if (!GetSaveFileNameW(ref ofn))
            return null;

        var result = TrimAtFirstNull(ofn.lpstrFile);

        // Ensure extension if user omitted it and a defaultExtension was provided.
        if (!string.IsNullOrWhiteSpace(defaultExtension) &&
            !Path.HasExtension(result))
        {
            result += "." + defaultExtension.TrimStart('.');
        }

        return result;
    }

    // --- Win32 interop ---

    // Common dialog flags
    private const int OFN_READONLY = 0x00000001;
    private const int OFN_OVERWRITEPROMPT = 0x00000002;
    private const int OFN_HIDEREADONLY = 0x00000004;
    private const int OFN_NOCHANGEDIR = 0x00000008;
    private const int OFN_PATHMUSTEXIST = 0x00000800;
    private const int OFN_FILEMUSTEXIST = 0x00001000;
    private const int OFN_EXPLORER = 0x00080000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OPENFILENAMEW
    {
        public int lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public string lpstrFilter;
        public string lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public string lpstrFile;
        public int nMaxFile;
        public string lpstrFileTitle;
        public int nMaxFileTitle;
        public string lpstrInitialDir;
        public string lpstrTitle;
        public int Flags;
        public short nFileOffset;
        public short nFileExtension;
        public string lpstrDefExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public string lpTemplateName;
        public IntPtr pvReserved;
        public int dwReserved;
        public int FlagsEx;
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetOpenFileNameW(ref OPENFILENAMEW ofn);

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetSaveFileNameW(ref OPENFILENAMEW ofn);

    private static OPENFILENAMEW CreateBaseOFN(string title, string initialDirectory, ExtensionFilter[] filters, string defaultExtension)
    {
        var ofn = new OPENFILENAMEW
        {
            lStructSize = Marshal.SizeOf<OPENFILENAMEW>(),
            hwndOwner = IntPtr.Zero,
            hInstance = IntPtr.Zero,
            lpstrFilter = BuildWin32FilterString(filters),
            lpstrCustomFilter = null,
            nMaxCustFilter = 0,
            nFilterIndex = 1,
            lpstrFile = null,
            nMaxFile = 0,
            lpstrFileTitle = null,
            nMaxFileTitle = 0,
            lpstrInitialDir = string.IsNullOrWhiteSpace(initialDirectory) ? null : initialDirectory,
            lpstrTitle = string.IsNullOrWhiteSpace(title) ? null : title,
            Flags = 0,
            lpstrDefExt = string.IsNullOrWhiteSpace(defaultExtension) ? null : defaultExtension.TrimStart('.'),
            pvReserved = IntPtr.Zero,
            dwReserved = 0,
            FlagsEx = 0
        };

        return ofn;
    }

    private static string BuildWin32FilterString(ExtensionFilter[] filters)
    {
        // Win32 expects: "Display\0Pattern\0Display\0Pattern\0\0"
        if (filters == null || filters.Length == 0)
            return "All Files (*.*)\0*.*\0\0";

        var sb = new StringBuilder(256);

        foreach (var f in filters)
        {
            var display = string.IsNullOrWhiteSpace(f.Name) ? "Files" : f.Name;

            string pattern;
            if (f.Extensions == null || f.Extensions.Length == 0)
            {
                pattern = "*.*";
            }
            else if (f.Extensions.Any(e => e == "*" || e == "*.*"))
            {
                pattern = "*.*";
            }
            else
            {
                // extensions come in like "csv" -> "*.csv"
                pattern = string.Join(";", f.Extensions
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e.StartsWith("*.") ? e : e.StartsWith(".") ? "*" + e : "*." + e));
            }

            sb.Append(display);
            sb.Append('\0');
            sb.Append(pattern);
            sb.Append('\0');
        }

        sb.Append('\0'); // double-null terminate
        return sb.ToString();
    }

    private static string TrimAtFirstNull(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var idx = s.IndexOf('\0');
        return idx >= 0 ? s.Substring(0, idx) : s;
    }

    private static string WriteIntoNullTerminatedBuffer(string buffer, string value)
    {
        if (buffer == null) return value ?? string.Empty;
        value ??= string.Empty;

        // Put "value\0" at the front, rest remains nulls.
        var chars = buffer.ToCharArray();
        var toCopy = Math.Min(value.Length, chars.Length - 1);
        for (int i = 0; i < toCopy; i++)
            chars[i] = value[i];
        chars[toCopy] = '\0';
        for (int i = toCopy + 1; i < chars.Length; i++)
            chars[i] = '\0';

        return new string(chars);
    }
}
#endif
