@echo off
setlocal

set "OUTPUT_DIR=%~dp0Wallpaper_Engine"

echo Building Wallpaper Manager release...
echo Output folder: %OUTPUT_DIR%
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo ERROR: The .NET SDK was not found.
    echo Install the .NET SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

if exist "%OUTPUT_DIR%" (
    echo Removing old output folder...
    rmdir /s /q "%OUTPUT_DIR%"
)

dotnet publish "%~dp0WallpaperManager.csproj" -c Release -r win-x64 --self-contained false -o "%OUTPUT_DIR%"
if errorlevel 1 (
    echo.
    echo Build failed.
    pause
    exit /b 1
)

echo.
echo Build complete.
echo Run this file:
echo %OUTPUT_DIR%\WallpaperManager.exe
echo.
pause
