using System.Reflection;

namespace Origins.Launcher;

internal sealed class SplashForm : Form
{
    private readonly ProgressBar _progress = new() { Height = 6, Dock = DockStyle.Bottom, Style = ProgressBarStyle.Continuous };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Color.FromArgb(190, 205, 225), Text = "กำลังเตรียมพร้อม..." };
    private readonly Button _start = CreateButton("PLAY NOW", Color.FromArgb(226, 91, 35), 158);
    private readonly Button _settings = CreateButton("⚙  SETTINGS", Color.FromArgb(35, 52, 76), 132);
    private readonly Label _guardState = new() { AutoSize = true, ForeColor = Color.FromArgb(103, 220, 160), Text = "●  ORIGINS GUARD READY" };
    private readonly NotifyIcon _guardIcon = new() { Text = "MU Origins Guard", Visible = false };
    private readonly string _directory;

    public SplashForm(string directory, LauncherSettings launcherSettings)
    {
        _directory = directory;
        Text = "MU Origins Launcher";
        ClientSize = new Size(820, 500);
        MinimumSize = MaximumSize = Size;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(10, 16, 27);
        DoubleBuffered = true;
        LoadBrandAssets();

        var hero = new Panel { Dock = DockStyle.Fill, BackgroundImage = BackgroundImage, BackgroundImageLayout = ImageLayout.Zoom };
        var shade = new Panel { Dock = DockStyle.Bottom, Height = 160, BackColor = Color.FromArgb(238, 10, 16, 27) };
        hero.Controls.Add(shade);
        shade.Controls.Add(new Label { AutoSize = true, Text = "MU ORIGINS", ForeColor = Color.White, Font = new Font("Segoe UI", 22, FontStyle.Bold), Location = new Point(28, 18) });
        shade.Controls.Add(new Label { AutoSize = true, Text = "SEASON 6 EPISODE 3  •  OFFICIAL CLIENT", ForeColor = Color.FromArgb(101, 184, 255), Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(32, 59) });
        shade.Controls.Add(new Label { AutoSize = true, Text = "Launcher 1.1  |  Auto Update", ForeColor = Color.FromArgb(135, 150, 170), Location = new Point(32, 87) });

        _guardState.Location = new Point(32, 120); _status.Location = new Point(32, 140);
        shade.Controls.Add(_guardState); shade.Controls.Add(_status);
        _settings.Location = new Point(500, 82); _start.Location = new Point(640, 82); _start.Enabled = false;
        shade.Controls.Add(_settings); shade.Controls.Add(_start); shade.Controls.Add(_progress);
        _start.Click += (_, _) => StartRequested?.Invoke(this, EventArgs.Empty);
        _settings.Click += (_, _) => EditSettings();
        Controls.Add(hero);
    }

    public event EventHandler? StartRequested;
    public void SetStatus(string status, int progress) { _status.Text = status; _progress.Value = Math.Clamp(progress, 0, 100); }
    public void SetReady() { SetStatus("พร้อมเข้าเกม — ระบบอัปเดตสมบูรณ์", 100); _start.Enabled = true; _start.Focus(); }
    public void ShowGuardNotification() { _guardIcon.Visible = true; _guardIcon.ShowBalloonTip(2500, "MU Origins Guard", "กำลังป้องกันเกมอยู่เบื้องหลัง", ToolTipIcon.Info); }
    public void HideGuardNotification() => _guardIcon.Visible = false;
    protected override void Dispose(bool disposing) { if (disposing) _guardIcon.Dispose(); base.Dispose(disposing); }

    private void EditSettings()
    {
        using var dialog = new SettingsForm(GameSettings.Load(_directory));
        if (dialog.ShowDialog(this) == DialogResult.OK)
            MessageBox.Show(this, "บันทึกการตั้งค่าเรียบร้อยแล้ว", "MU Origins", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void LoadBrandAssets()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var backgroundName = assembly.GetManifestResourceNames().First(n => n.EndsWith("aegis-launcher.jpe", StringComparison.OrdinalIgnoreCase));
        using (var stream = assembly.GetManifestResourceStream(backgroundName)!) BackgroundImage = new Bitmap(stream);
        var iconName = assembly.GetManifestResourceNames().First(n => n.EndsWith("aegis-icon.ico", StringComparison.OrdinalIgnoreCase));
        using var iconStream = assembly.GetManifestResourceStream(iconName)!;
        Icon = new Icon(iconStream); _guardIcon.Icon = new Icon(Icon, 32, 32);
    }

    private static Button CreateButton(string text, Color color, int width) => new()
    {
        Text = text, Width = width, Height = 42, FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = Color.White,
        Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
    };
}
