namespace WallpaperManager;

internal static class Program
{
    private const string SingleInstanceMutexName = @"Global\WallpaperManager.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        using var appContext = new TrayApplicationContext();
        Application.Run(appContext);
    }
}
