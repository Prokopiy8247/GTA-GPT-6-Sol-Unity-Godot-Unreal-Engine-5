@echo off
setlocal EnableExtensions
set "PROJECT=%~dp0Unreal_GTA_GPT6Sol.uproject"
set "EDITOR="

if defined UE_ENGINE_ROOT if exist "%UE_ENGINE_ROOT%\Engine\Binaries\Win64\UnrealEditor.exe" set "EDITOR=%UE_ENGINE_ROOT%\Engine\Binaries\Win64\UnrealEditor.exe"

if not defined EDITOR (
  for %%D in ("%ProgramFiles%\Epic Games\UE_5.8" "%ProgramFiles%\Epic Games\UE_5.8.2") do (
    if exist "%%~D\Engine\Binaries\Win64\UnrealEditor.exe" set "EDITOR=%%~D\Engine\Binaries\Win64\UnrealEditor.exe"
  )
)

if not defined EDITOR (
  for /f "tokens=1,2,*" %%A in ('reg query "HKCU\SOFTWARE\Epic Games\Unreal Engine\Builds" 2^>nul ^| find "REG_SZ"') do (
    if exist "%%C\Engine\Binaries\Win64\UnrealEditor.exe" set "EDITOR=%%C\Engine\Binaries\Win64\UnrealEditor.exe"
  )
)

if not defined EDITOR (
  for %%V in (5.8 5.8.2) do (
    for /f "tokens=1,2,*" %%A in ('reg query "HKLM\SOFTWARE\EpicGames\Unreal Engine\%%V" /v InstalledDirectory 2^>nul ^| find "REG_SZ"') do (
      if exist "%%C\Engine\Binaries\Win64\UnrealEditor.exe" set "EDITOR=%%C\Engine\Binaries\Win64\UnrealEditor.exe"
    )
  )
)

if not defined EDITOR (
  echo.
  echo [ERROR] Unreal Engine 5.8 was not found.
  echo Install UE 5.8 through Epic Games Launcher, then run this file again.
  echo For a custom location, set UE_ENGINE_ROOT to the UE_5.8 folder.
  echo See README.md for the one-time C++ setup.
  echo.
  pause
  exit /b 1
)

echo Starting Harbor City with:
echo %EDITOR%
if /I "%~1"=="--check" (
  echo Launcher check passed.
  exit /b 0
)
start "Harbor City" "%EDITOR%" "%PROJECT%" -game -log
