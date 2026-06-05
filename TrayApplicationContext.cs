namespace WallpaperManager;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly SettingsService settingsService = new();
    private readonly NotifyIcon notifyIcon;
    private readonly System.Windows.Forms.Timer timer;
    private readonly WallpaperService wallpaperService;
    private readonly Icon appIcon;
    private AppSettings settings;
    private SettingsForm? settingsForm;
    private bool isPaused;

    public TrayApplicationContext()
    {
        settings = settingsService.Load();
        wallpaperService = new WallpaperService(settings);

        timer = new System.Windows.Forms.Timer();
        timer.Tick += (_, _) => ChangeWallpaper();
        appIcon = LoadAppIcon();

        notifyIcon = new NotifyIcon
        {
            Icon = appIcon,
            Text = "Wallpaper Manager",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };

        notifyIcon.DoubleClick += (_, _) => ShowSettings();

        ApplyTimerInterval();
        timer.Start();
        ChangeWallpaper();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add("Next wallpaper", null, (_, _) => ChangeWallpaper());
        menu.Items.Add("Pause slideshow", null, TogglePause);
        menu.Items.Add("Reshuffle", null, (_, _) =>
        {
            wallpaperService.Reshuffle();
            ShowTrayMessage("Wallpaper Manager", "Wallpaper order reshuffled.");
        });
        menu.Items.Add("Settings", null, (_, _) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());

        menu.Opening += (_, _) =>
        {
            if (menu.Items[1] is ToolStripMenuItem pauseItem)
            {
                pauseItem.Text = isPaused ? "Resume slideshow" : "Pause slideshow";
            }
        };

        return menu;
    }

    private void TogglePause(object? sender, EventArgs e)
    {
        isPaused = !isPaused;
        timer.Enabled = !isPaused;
        ShowTrayMessage("Wallpaper Manager", isPaused ? "Slideshow paused." : "Slideshow resumed.");
    }

    private void ShowSettings()
    {
        if (settingsForm is { IsDisposed: false })
        {
            settingsForm.Show();
            settingsForm.WindowState = FormWindowState.Normal;
            settingsForm.Activate();
            return;
        }

        settingsForm = new SettingsForm(settings.Clone());
        settingsForm.SettingsSaved += (_, newSettings) =>
        {
            settings = newSettings.Clone();
            settingsService.Save(settings);
            wallpaperService.ApplySettings(settings);
            ApplyTimerInterval();
            ChangeWallpaper();

            if (!isPaused)
            {
                timer.Start();
            }

            ShowTrayMessage("Wallpaper Manager", "Settings saved.");
        };

        settingsForm.Show();
        settingsForm.Activate();
    }

    private void ApplyTimerInterval()
    {
        timer.Interval = settings.IntervalSeconds * 1000;
    }

    private void ChangeWallpaper()
    {
        if (wallpaperService.TrySetNextWallpaper(out var message))
        {
            notifyIcon.Text = TrimNotifyText($"Wallpaper Manager - {message}");
            return;
        }

        notifyIcon.Text = "Wallpaper Manager";
        ShowTrayMessage("Wallpaper Manager", message);
    }

    private void ShowTrayMessage(string title, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        notifyIcon.BalloonTipTitle = title;
        notifyIcon.BalloonTipText = message;
        notifyIcon.ShowBalloonTip(2500);
    }

    private void ExitApplication()
    {
        timer.Stop();
        notifyIcon.Visible = false;
        settingsForm?.Close();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Dispose();
            notifyIcon.Dispose();
            appIcon.Dispose();
            settingsForm?.Dispose();
        }

        base.Dispose(disposing);
    }

    private static string TrimNotifyText(string text)
    {
        return text.Length <= 63 ? text : string.Concat(text.AsSpan(0, 60), "...");
    }

    private static Icon LoadAppIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "icon.ico");
        return File.Exists(iconPath)
            ? new Icon(iconPath)
            : SystemIcons.Application;
    }
}
