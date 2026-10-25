namespace Origins.Launcher;

internal sealed class SplashForm : Form
{
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Bottom, Height = 16 };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 28, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(18, 24, 36), ForeColor = Color.White };

    public SplashForm(string directory)
    {
        Text = "MU Origins Launcher";
        ClientSize = new Size(550, 300);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        BackgroundImageLayout = ImageLayout.Zoom;
        var image = Path.Combine(directory, "launcher-assets", "origins-guard.jpe");
        if (File.Exists(image)) BackgroundImage = Image.FromFile(image);
        Controls.Add(_status);
        Controls.Add(_progress);
    }

    public void SetStatus(string status, int progress)
    {
        _status.Text = status;
        _progress.Value = Math.Clamp(progress, 0, 100);
    }
}
