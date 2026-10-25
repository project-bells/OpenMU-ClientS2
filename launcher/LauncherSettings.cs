namespace Origins.Launcher;

internal sealed class LauncherSettings
{
    public string ManifestUrl { get; } = "https://muorigins.online/client/manifest.json";
    public string GameExecutable { get; } = "Main.exe";
    public int ScanIntervalSeconds { get; } = 60;
    public string[] BlockedProcesses { get; } = ["cheatengine", "cheatengine-x86_64", "ollydbg", "x64dbg", "x32dbg"];

    public static LauncherSettings Load(string directory)
    {
        // Production endpoints and guard policy are compiled into the signed launcher.
        // Local JSON configuration is deliberately ignored to prevent redirection.
        return new();
    }

}
