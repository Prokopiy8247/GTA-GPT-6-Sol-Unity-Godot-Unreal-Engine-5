@echo off
setlocal
cd /d "%~dp0"

if exist "build\windows\Harborline.exe" (
  start "" "build\windows\Harborline.exe"
  exit /b 0
)

where godot >nul 2>nul
if %errorlevel% equ 0 (
  godot --path "%~dp0"
  exit /b %errorlevel%
)

where godot4 >nul 2>nul
if %errorlevel% equ 0 (
  godot4 --path "%~dp0"
  exit /b %errorlevel%
)

echo Godot 4.7.2 was not found.
echo Install it from https://godotengine.org/download/archive/4.7.2-stable/
echo or download the ready Windows build from this repository's Releases page.
pause
exit /b 1
