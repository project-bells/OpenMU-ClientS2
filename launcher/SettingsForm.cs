namespace Origins.Launcher;

internal sealed class SettingsForm : Form
{
    private readonly GameSettings _settings;
    private readonly ComboBox _resolution = Picker(), _display = Picker(), _uiScale = Picker(), _antialiasing = Picker();
    private readonly TrackBar _sound = Slider(), _music = Slider();
    private readonly CheckBox _vsync = new() { Text = "เปิด VSync (ลดภาพฉีก)", AutoSize = true, ForeColor = Color.White };

    public SettingsForm(GameSettings settings)
    {
        _settings = settings;
        Text = "Origins Settings"; ClientSize = new Size(520, 490); FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false; StartPosition = FormStartPosition.CenterParent; BackColor = Color.FromArgb(13, 21, 34);
        _resolution.Items.AddRange(["800 × 600", "1024 × 768", "1280 × 720", "1366 × 768", "1600 × 900", "1920 × 1080", "2560 × 1440"]);
        _display.Items.AddRange(["Windowed (หน้าต่าง)", "Fullscreen (เต็มจอ)"]);
        _uiScale.Items.AddRange(["75%", "100%", "125%", "150%"]);
        _antialiasing.Items.AddRange(["ปิด", "2×", "4×", "8×"]);
        Controls.Add(new Label { Text = "GAME SETTINGS", ForeColor = Color.White, Font = new Font("Segoe UI", 18, FontStyle.Bold), AutoSize = true, Location = new Point(28, 20) });
        Controls.Add(new Label { Text = "ตั้งค่าภาพและเสียงสำหรับเครื่องนี้", ForeColor = Color.FromArgb(145, 165, 190), AutoSize = true, Location = new Point(31, 58) });
        AddRow("ความละเอียด", _resolution, 95); AddRow("โหมดหน้าจอ", _display, 145); AddRow("เสียงเอฟเฟกต์", _sound, 195);
        AddRow("เสียงเพลง", _music, 245); AddRow("ขนาด UI", _uiScale, 295); AddRow("Anti-aliasing", _antialiasing, 345);
        _vsync.Location = new Point(190, 397); Controls.Add(_vsync);
        var save = new Button { Text = "SAVE SETTINGS", Width = 150, Height = 38, Location = new Point(340, 438), BackColor = Color.FromArgb(226, 91, 35), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        save.FlatAppearance.BorderSize = 0; save.Click += (_, _) => SaveAndClose(); Controls.Add(save); AcceptButton = save;
        LoadValues();
    }

    private void LoadValues()
    {
        _resolution.Text = $"{_settings.Width} × {_settings.Height}"; _display.SelectedIndex = _settings.Windowed ? 0 : 1;
        _sound.Value = Math.Clamp(_settings.SoundVolume, 0, 10); _music.Value = Math.Clamp(_settings.MusicVolume, 0, 10);
        _uiScale.Text = $"{_settings.UiScale}%"; _antialiasing.Text = _settings.Antialiasing == 0 ? "ปิด" : $"{_settings.Antialiasing}×"; _vsync.Checked = _settings.VSync;
    }
    private void SaveAndClose()
    {
        var parts = _resolution.Text.Replace(" ", "").Split('×');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var width) || !int.TryParse(parts[1], out var height)) return;
        _settings.Width = width; _settings.Height = height; _settings.Windowed = _display.SelectedIndex != 1;
        _settings.SoundVolume = _sound.Value; _settings.MusicVolume = _music.Value; _settings.VSync = _vsync.Checked;
        _settings.UiScale = int.TryParse(_uiScale.Text.TrimEnd('%'), out var scale) ? scale : 100;
        _settings.Antialiasing = _antialiasing.SelectedIndex switch { 1 => 2, 2 => 4, 3 => 8, _ => 0 };
        _settings.Save(); DialogResult = DialogResult.OK; Close();
    }
    private void AddRow(string label, Control control, int y)
    {
        Controls.Add(new Label { Text = label, ForeColor = Color.FromArgb(205, 215, 230), AutoSize = true, Location = new Point(31, y + 8) });
        control.Location = new Point(190, y); control.Width = 300; Controls.Add(control);
    }
    private static ComboBox Picker() => new() { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Height = 30 };
    private static TrackBar Slider() => new() { Minimum = 0, Maximum = 10, TickFrequency = 1, Height = 36 };
}
