using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WallpaperManager;

public sealed class WallpaperService
{
    private const int SpiSetDesktopWallpaper = 0x0014;
    private const int SpifUpdateIniFile = 0x01;
    private const int SpifSendWinIniChange = 0x02;
    private static readonly Guid DesktopWallpaperClsid = new("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD");

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".bmp",
        ".webp",
        ".avif"
    };

    private readonly Random random = new();
    private List<string> images = [];
    private int index;
    private AppSettings settings;

    public WallpaperService(AppSettings settings)
    {
        this.settings = settings.Clone();
        ReloadImages();
    }

    public int ImageCount => images.Count;

    public string? CurrentWallpaper { get; private set; }

    public void ApplySettings(AppSettings newSettings)
    {
        var folderChanged = !string.Equals(settings.WallpaperFolder, newSettings.WallpaperFolder, StringComparison.OrdinalIgnoreCase);
        var shuffleChanged = settings.ShuffleEnabled != newSettings.ShuffleEnabled;
        var displayModeChanged = settings.DisplayMode != newSettings.DisplayMode;

        settings = newSettings.Clone();
        settings.Normalize();

        if (folderChanged || shuffleChanged || displayModeChanged)
        {
            ReloadImages();
        }
    }

    public void ReloadImages()
    {
        images = ScanImages(settings.WallpaperFolder);

        if (settings.ShuffleEnabled)
        {
            Shuffle(images);
        }
        else
        {
            images.Sort(StringComparer.OrdinalIgnoreCase);
        }

        index = 0;
    }

    public void Reshuffle()
    {
        if (settings.ShuffleEnabled)
        {
            Shuffle(images);
        }
        else
        {
            images.Sort(StringComparer.OrdinalIgnoreCase);
        }

        index = 0;
    }

    public bool TrySetNextWallpaper(out string message)
    {
        message = string.Empty;

        if (!Directory.Exists(settings.WallpaperFolder))
        {
            images.Clear();
            index = 0;
            message = "Wallpaper folder is missing or invalid.";
            return false;
        }

        if (images.Count == 0)
        {
            ReloadImages();
        }

        if (images.Count == 0)
        {
            message = "No supported images were found in the selected folder.";
            return false;
        }

        if (settings.DisplayMode == WallpaperDisplayMode.DifferentPerMonitor)
        {
            return TrySetDifferentWallpapers(out message);
        }

        if (TryGetNextExistingImage(out var candidate))
        {
            try
            {
                SetWallpaper(candidate);
                CurrentWallpaper = candidate;
                message = Path.GetFileName(candidate);
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
        }

        ReloadImages();
        message = "No available image could be applied.";
        return false;
    }

    private bool TrySetDifferentWallpapers(out string message)
    {
        message = string.Empty;

        try
        {
            var desktopWallpaper = CreateDesktopWallpaper();
            desktopWallpaper.GetMonitorDevicePathCount(out var monitorCount);

            if (monitorCount == 0)
            {
                return TrySetNextSharedWallpaper(out message);
            }

            var appliedCount = 0;
            for (uint i = 0; i < monitorCount; i++)
            {
                if (!TryGetNextExistingImage(out var candidate))
                {
                    break;
                }

                desktopWallpaper.GetMonitorDevicePathAt(i, out var monitorId);
                desktopWallpaper.SetWallpaper(monitorId, candidate);
                CurrentWallpaper = candidate;
                appliedCount++;
            }

            message = appliedCount > 0
                ? $"Updated {appliedCount} monitor wallpaper{(appliedCount == 1 ? string.Empty : "s")}."
                : "No available image could be applied.";

            return appliedCount > 0;
        }
        catch (Exception ex)
        {
            if (TrySetNextSharedWallpaper(out message))
            {
                message = $"Per-monitor mode is unavailable, so a shared wallpaper was applied. {ex.Message}";
                return true;
            }

            message = $"Per-monitor mode is unavailable. {ex.Message}";
            return false;
        }
    }

    private bool TrySetNextSharedWallpaper(out string message)
    {
        if (TryGetNextExistingImage(out var candidate))
        {
            SetWallpaper(candidate);
            CurrentWallpaper = candidate;
            message = Path.GetFileName(candidate);
            return true;
        }

        message = "No available image could be applied.";
        return false;
    }

    private bool TryGetNextExistingImage(out string path)
    {
        path = string.Empty;
        var attempts = images.Count;

        while (attempts-- > 0)
        {
            if (index >= images.Count)
            {
                Reshuffle();
            }

            var candidate = images[index++];
            if (File.Exists(candidate))
            {
                path = candidate;
                return true;
            }

            images.Remove(candidate);
            index = Math.Max(0, index - 1);
        }

        return false;
    }

    private static List<string> ScanImages(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return [];
        }

        var foundImages = new List<string>();
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(folder);

        while (pendingDirectories.Count > 0)
        {
            var currentDirectory = pendingDirectories.Pop();

            try
            {
                foreach (var path in Directory.EnumerateFiles(currentDirectory))
                {
                    if (SupportedExtensions.Contains(Path.GetExtension(path)))
                    {
                        foundImages.Add(path);
                    }
                }
            }
            catch
            {
                continue;
            }

            try
            {
                foreach (var directory in Directory.EnumerateDirectories(currentDirectory))
                {
                    pendingDirectories.Push(directory);
                }
            }
            catch
            {
                // Keep the rest of the slideshow usable even when one subfolder is restricted.
            }
        }

        return foundImages;
    }

    private void Shuffle(List<string> items)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    private static void SetWallpaper(string path)
    {
        var result = SystemParametersInfo(
            SpiSetDesktopWallpaper,
            0,
            path,
            SpifUpdateIniFile | SpifSendWinIniChange);

        if (!result)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows could not apply this wallpaper.");
        }
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SystemParametersInfo(int uiAction, int uiParam, string pvParam, int fWinIni);

    private static IDesktopWallpaper CreateDesktopWallpaper()
    {
        var desktopWallpaperType = Type.GetTypeFromCLSID(DesktopWallpaperClsid, throwOnError: true)
            ?? throw new InvalidOperationException("Windows desktop wallpaper service could not be found.");

        return (IDesktopWallpaper)Activator.CreateInstance(desktopWallpaperType)!;
    }

    [ComImport]
    [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDesktopWallpaper
    {
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);

        void GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId, [MarshalAs(UnmanagedType.LPWStr)] out string wallpaper);

        void GetMonitorDevicePathAt(uint monitorIndex, [MarshalAs(UnmanagedType.LPWStr)] out string monitorId);

        void GetMonitorDevicePathCount(out uint count);

        void GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorId, out Rectangle displayRect);

        void SetBackgroundColor(uint color);

        void GetBackgroundColor(out uint color);

        void SetPosition(DesktopWallpaperPosition position);

        void GetPosition(out DesktopWallpaperPosition position);

        void SetSlideshow(IntPtr items);

        void GetSlideshow(out IntPtr items);

        void SetSlideshowOptions(uint options, uint slideshowTick);

        void GetSlideshowOptions(out uint options, out uint slideshowTick);

        void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string? monitorId, DesktopSlideshowDirection direction);

        void GetStatus(out uint state);

        void Enable(bool enable);
    }

    private enum DesktopWallpaperPosition
    {
        Center = 0,
        Tile = 1,
        Stretch = 2,
        Fit = 3,
        Fill = 4,
        Span = 5
    }

    private enum DesktopSlideshowDirection
    {
        Forward = 0,
        Backward = 1
    }
}
