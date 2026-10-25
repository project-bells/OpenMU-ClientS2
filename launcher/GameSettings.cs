using System.Runtime.InteropServices;
using System.Text;

namespace Origins.Launcher;

internal sealed class GameSettings
{
    private readonly string _path;
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Windowed { get; set; }
    public int SoundVolume { get; set; }
    public int MusicVolume { get; set; }
    public bool VSync { get; set; }
    public int Antialiasing { get; set; }
    public int UiScale { get; set; }

    private GameSettings(string path) => _path = path;

    public static GameSettings Load(string directory)
    {
        var settings = new GameSettings(Path.Combine(directory, "config.ini"));
        settings.Width = settings.ReadInt("Window", "Width", 1024);
        settings.Height = settings.ReadInt("Window", "Height", 768);
        settings.Windowed = settings.ReadInt("Window", "Windowed", 1) != 0;
        settings.SoundVolume = settings.ReadInt("Audio", "SoundVolume", 5);
        settings.MusicVolume = settings.ReadInt("Audio", "MusicVolume", 5);
        settings.VSync = settings.ReadInt("Render", "VSync", 1) != 0;
        settings.Antialiasing = settings.ReadInt("Render", "Antialiasing", 4);
        settings.UiScale = settings.ReadInt("UI", "UIScalePercent", 100);
        return settings;
    }

    public void Save()
    {
        if (File.Exists(_path)) File.SetAttributes(_path, FileAttributes.Normal);
        Write("Window", "Width", Width); Write("Window", "Height", Height); Write("Window", "Windowed", Windowed ? 1 : 0);
        Write("Audio", "SoundVolume", SoundVolume); Write("Audio", "MusicVolume", MusicVolume);
        Write("Render", "VSync", VSync ? 1 : 0); Write("Render", "Antialiasing", Antialiasing);
        Write("UI", "UIScalePercent", UiScale);
        WritePrivateProfileString("CONNECTION SETTINGS", null, null, _path);
        File.SetAttributes(_path, File.GetAttributes(_path) | FileAttributes.Hidden);
    }

    private int ReadInt(string section, string key, int fallback) => GetPrivateProfileInt(section, key, fallback, _path);
    private void Write(string section, string key, int value) => WritePrivateProfileString(section, key, value.ToString(), _path);

    [DllImport("kernel32", CharSet = CharSet.Unicode)] private static extern int GetPrivateProfileInt(string section, string key, int fallback, string path);
    [DllImport("kernel32", CharSet = CharSet.Unicode)] private static extern bool WritePrivateProfileString(string section, string? key, string? value, string path);
}
