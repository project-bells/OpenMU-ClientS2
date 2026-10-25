using System.Text.Json;

namespace Origins.Launcher;

internal sealed class LauncherSettings
{
    public string ManifestUrl { get; set; } = "https://muorigins.online/client/manifest.json";
    public string GameExecutable { get; set; } = "main.exe";
    public int ScanIntervalSeconds { get; set; } = 60;
    public string[] BlockedProcesses { get; set; } = ["cheatengine", "cheatengine-x86_64", "ollydbg", "x64dbg", "x32dbg"];

    public static LauncherSettings Load(string directory)
    {
        var path = Path.Combine(directory, "launcher.settings.json");
        return File.Exists(path)
            ? JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(path)) ?? new()
            : new();
    }
}
