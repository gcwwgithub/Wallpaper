# Wallpaper Manager

A Windows system tray wallpaper manager built with C# WinForms.

It can load wallpapers recursively from a selected folder, rotate them on a timer, shuffle without repeats until a full cycle completes, and optionally use different wallpapers on each monitor.

## Requirements

- Windows 10 or Windows 11
- .NET SDK installed

Download the .NET SDK from:

```text
https://dotnet.microsoft.com/download
```

## Build The Release App

Double-click:

```text
build_release.bat
```

Or run it from PowerShell:

```powershell
.\build_release.bat
```

The script creates this folder:

```text
Wallpaper_Engine
```

The app exe will be here:

```text
Wallpaper_Engine\WallpaperManager.exe
```

## Run

Double-click:

```text
Wallpaper_Engine\WallpaperManager.exe
```

The app starts in the system tray. Right-click the tray icon to open the menu.

## Settings

Open `Settings` from the tray menu to configure:

- Wallpaper folder
- Timer interval
- Shuffle mode
- Same wallpaper on all monitors or different wallpaper per monitor

Settings are saved automatically to your local Windows app data folder.

## Run On Startup

Press `Win + R`, then enter:

```text
shell:startup
```

Create a shortcut to:

```text
Wallpaper_Engine\WallpaperManager.exe
```

Place that shortcut in the Startup folder.

## Stop The App

Use `Exit` from the tray menu, or run:

```powershell
Stop-Process -Name WallpaperManager -Force
```
