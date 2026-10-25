using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;

namespace Origins.Launcher;

internal static class Program
{
    private const string GuardEnvironment = "ORIGINS_GUARD_PID";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var directory = AppContext.BaseDirectory;
        var settings = LauncherSettings.Load(directory);
        using var form = new SplashForm(directory, settings);
        form.Shown += async (_, _) => await PrepareAsync(form, directory, settings);
        form.StartRequested += async (_, _) => await StartGameAsync(form, directory, settings);
        Application.Run(form);
    }

    private static async Task PrepareAsync(SplashForm form, string directory, LauncherSettings settings)
    {
        try
        {
            await UpdateAsync(form, directory, settings.ManifestUrl);
            form.SetReady();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Origins Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static async Task StartGameAsync(SplashForm form, string directory, LauncherSettings settings)
    {
        try
        {
            form.SetStatus("กำลังเริ่ม Origins Guard...", 100);
            await GuardGameAsync(directory, settings, form);
        }
        catch (Exception exception)
        {
            form.Show();
            form.SetReady();
            MessageBox.Show(exception.Message, "Origins Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static async Task UpdateAsync(SplashForm form, string directory, string manifestUrl)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        form.SetStatus("กำลังตรวจสอบอัปเดต...", 5);
        using var response = await client.GetAsync(manifestUrl);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            form.SetStatus("ยังไม่มีแพตช์บนเซิร์ฟเวอร์ กำลังเปิดเกม...", 90);
            return;
        }

        response.EnsureSuccessStatusCode();
        var manifest = JsonSerializer.Deserialize<UpdateManifest>(await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidDataException("Manifest ไม่ถูกต้อง");
        for (var index = 0; index < manifest.Files.Count; index++)
        {
            var file = manifest.Files[index];
            var destination = SafePath(directory, file.Path);
            if (!File.Exists(destination) || !string.Equals(await HashAsync(destination), file.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                var temporary = destination + ".download";
                await File.WriteAllBytesAsync(temporary, await client.GetByteArrayAsync(new Uri(new Uri(manifest.BaseUrl), file.Path)));
                if (!string.Equals(await HashAsync(temporary), file.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"SHA-256 ไม่ตรง: {file.Path}");
                File.Move(temporary, destination, true);
            }
            form.SetStatus($"ตรวจสอบ {file.Path}", 5 + (int)(85.0 * (index + 1) / Math.Max(1, manifest.Files.Count)));
        }
    }

    private static async Task GuardGameAsync(string directory, LauncherSettings settings, SplashForm form)
    {
        var executable = SafePath(directory, settings.GameExecutable);
        if (!File.Exists(executable)) throw new FileNotFoundException("ไม่พบ main.exe", executable);
        var start = new ProcessStartInfo(executable) { WorkingDirectory = directory, UseShellExecute = false };
        start.Environment[GuardEnvironment] = Environment.ProcessId.ToString();
        using var game = Process.Start(start) ?? throw new InvalidOperationException("เปิดเกมไม่สำเร็จ");
        var logPath = Path.Combine(directory, "OriginsGuard.log");
        File.AppendAllText(logPath, $"{DateTimeOffset.Now:u} guard-start game-pid={game.Id}{Environment.NewLine}");
        form.Hide();
        form.ShowGuardNotification();
        while (!game.HasExited)
        {
            var blocked = Process.GetProcesses().FirstOrDefault(process =>
                settings.BlockedProcesses.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase));
            if (blocked is not null)
            {
                File.AppendAllText(logPath, $"{DateTimeOffset.Now:u} blocked={blocked.ProcessName}{Environment.NewLine}");
                game.Kill(true);
                break;
            }
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(10, settings.ScanIntervalSeconds)));
        }
        await game.WaitForExitAsync();
        File.AppendAllText(logPath, $"{DateTimeOffset.Now:u} guard-stop game-exit={game.ExitCode}{Environment.NewLine}");
        form.HideGuardNotification();
        form.Close();
    }

    private static string SafePath(string root, string relative)
    {
        var path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("พบ path ที่ไม่ปลอดภัยใน manifest");
        return path;
    }

    private static async Task<string> HashAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream));
    }
}
