namespace WallpaperManager;

public sealed class AppSettings
{
    public string WallpaperFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

    public int IntervalSeconds { get; set; } = 300;

    public bool ShuffleEnabled { get; set; } = true;

    public WallpaperDisplayMode DisplayMode { get; set; } = WallpaperDisplayMode.SameOnAllMonitors;

    public void Normalize()
    {
        WallpaperFolder ??= string.Empty;
        IntervalSeconds = Math.Clamp(IntervalSeconds, 5, 3600);

        if (!Enum.IsDefined(DisplayMode))
        {
            DisplayMode = WallpaperDisplayMode.SameOnAllMonitors;
        }
    }

    public AppSettings Clone()
    {
        return new AppSettings
        {
            WallpaperFolder = WallpaperFolder,
            IntervalSeconds = IntervalSeconds,
            ShuffleEnabled = ShuffleEnabled,
            DisplayMode = DisplayMode
        };
    }
}
