namespace WallpaperManager;

public sealed class SettingsForm : Form
{
    private readonly TextBox folderTextBox = new();
    private readonly Button browseButton = new();
    private readonly NumericUpDown intervalNumeric = new();
    private readonly CheckBox shuffleCheckBox = new();
    private readonly CheckBox differentPerMonitorCheckBox = new();
    private readonly Button saveButton = new();
    private readonly Button closeButton = new();

    public event EventHandler<AppSettings>? SettingsSaved;

    public SettingsForm(AppSettings settings)
    {
        Text = "Wallpaper Manager Settings";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(560, 238);

        BuildLayout();
        LoadSettings(settings);
    }

    private void BuildLayout()
    {
        var folderLabel = new Label
        {
            Text = "Wallpaper folder",
            AutoSize = true,
            Location = new Point(16, 20)
        };

        folderTextBox.Location = new Point(16, 45);
        folderTextBox.Size = new Size(440, 27);
        folderTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        browseButton.Text = "Browse";
        browseButton.Location = new Point(466, 44);
        browseButton.Size = new Size(90, 29);
        browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        browseButton.Click += (_, _) => BrowseForFolder();

        var intervalLabel = new Label
        {
            Text = "Timer interval (seconds)",
            AutoSize = true,
            Location = new Point(16, 92)
        };

        intervalNumeric.Location = new Point(180, 88);
        intervalNumeric.Size = new Size(120, 27);
        intervalNumeric.Minimum = 5;
        intervalNumeric.Maximum = 3600;
        intervalNumeric.Increment = 5;

        shuffleCheckBox.Text = "Shuffle mode enabled";
        shuffleCheckBox.AutoSize = true;
        shuffleCheckBox.Location = new Point(16, 130);

        differentPerMonitorCheckBox.Text = "Use a different wallpaper on each monitor";
        differentPerMonitorCheckBox.AutoSize = true;
        differentPerMonitorCheckBox.Location = new Point(16, 160);

        saveButton.Text = "Apply / Save";
        saveButton.Location = new Point(354, 193);
        saveButton.Size = new Size(110, 30);
        saveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        saveButton.Click += (_, _) => SaveSettings();

        closeButton.Text = "Close";
        closeButton.Location = new Point(474, 193);
        closeButton.Size = new Size(82, 30);
        closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        closeButton.Click += (_, _) => Hide();

        Controls.Add(folderLabel);
        Controls.Add(folderTextBox);
        Controls.Add(browseButton);
        Controls.Add(intervalLabel);
        Controls.Add(intervalNumeric);
        Controls.Add(shuffleCheckBox);
        Controls.Add(differentPerMonitorCheckBox);
        Controls.Add(saveButton);
        Controls.Add(closeButton);

        AcceptButton = saveButton;
        CancelButton = closeButton;
    }

    private void LoadSettings(AppSettings settings)
    {
        settings.Normalize();
        folderTextBox.Text = settings.WallpaperFolder;
        intervalNumeric.Value = settings.IntervalSeconds;
        shuffleCheckBox.Checked = settings.ShuffleEnabled;
        differentPerMonitorCheckBox.Checked = settings.DisplayMode == WallpaperDisplayMode.DifferentPerMonitor;
    }

    private void BrowseForFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select your wallpaper folder",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(folderTextBox.Text)
                ? folderTextBox.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            folderTextBox.Text = dialog.SelectedPath;
        }
    }

    private void SaveSettings()
    {
        var settings = new AppSettings
        {
            WallpaperFolder = folderTextBox.Text.Trim(),
            IntervalSeconds = (int)intervalNumeric.Value,
            ShuffleEnabled = shuffleCheckBox.Checked,
            DisplayMode = differentPerMonitorCheckBox.Checked
                ? WallpaperDisplayMode.DifferentPerMonitor
                : WallpaperDisplayMode.SameOnAllMonitors
        };

        settings.Normalize();
        SettingsSaved?.Invoke(this, settings);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnFormClosing(e);
    }
}
