using System.Reflection;

namespace Origins.Launcher;

internal sealed class SplashForm : Form
{
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Bottom, Height = 14 };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 28, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(18, 24, 36), ForeColor = Color.White };
    private readonly Button _start = new() { Text = "START GAME", Width = 135, Height = 36, Enabled = false };
    private readonly Button _settings = new() { Text = "SETTINGS", Width = 100, Height = 36 };
    private readonly NotifyIcon _guardIcon = new() { Text = "MU Origins Guard", Icon = SystemIcons.Shield, Visible = false };
    private readonly string _directory;
    private readonly LauncherSettings _launcherSettings;

    public SplashForm(string directory, LauncherSettings launcherSettings)
    {
        _directory = directory;
        _launcherSettings = launcherSettings;
        Text = "MU Origins Launcher";
        ClientSize = new Size(550, 340);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackgroundImageLayout = ImageLayout.Zoom;
        LoadEmbeddedBackground();
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8, 6, 8, 4), BackColor = Color.FromArgb(18, 24, 36) };
        buttons.Controls.Add(_start);
        buttons.Controls.Add(_settings);
        _start.Click += (_, _) => StartRequested?.Invoke(this, EventArgs.Empty);
        _settings.Click += (_, _) => EditSettings();
        Controls.Add(buttons);
        Controls.Add(_status);
        Controls.Add(_progress);
    }

    public event EventHandler? StartRequested;

    public void SetStatus(string status, int progress)
    {
        _status.Text = status;
        _progress.Value = Math.Clamp(progress, 0, 100);
    }

    public void SetReady()
    {
        SetStatus("พร้อมเข้าเกม", 100);
        _start.Enabled = true;
    }

    public void ShowGuardNotification()
    {
        _guardIcon.Visible = true;
        _guardIcon.ShowBalloonTip(2500, "MU Origins Guard", "กำลังป้องกันเกมอยู่เบื้องหลัง", ToolTipIcon.Info);
    }

    public void HideGuardNotification() => _guardIcon.Visible = false;

    protected override void Dispose(bool disposing)
    {
        if (disposing) _guardIcon.Dispose();
        base.Dispose(disposing);
    }

    private void EditSettings()
    {
        var value = Microsoft.VisualBasic.Interaction.InputBox("Manifest URL", "MU Origins Settings", _launcherSettings.ManifestUrl);
        if (string.IsNullOrWhiteSpace(value)) return;
        _launcherSettings.ManifestUrl = value.Trim();
        _launcherSettings.Save(_directory);
        MessageBox.Show("บันทึกการตั้งค่าแล้ว", "MU Origins", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void LoadEmbeddedBackground()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resource = assembly.GetManifestResourceNames().First(name => name.EndsWith("origins-guard.jpe", StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(resource)!;
        BackgroundImage = Image.FromStream(stream);
    }
}
