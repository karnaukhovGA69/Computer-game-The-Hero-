@echo off
setlocal enabledelayedexpansion

set "PROJECT=%~dp0.."
set "PROJECT=%PROJECT:~0,-1%"
set "LOG=%PROJECT%\Logs\BuildMainAssetsMap.log"

if not exist "%PROJECT%\Logs" mkdir "%PROJECT%\Logs"

set "UNITY="
for %%V in (
  "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe"
  "C:\Program Files\Unity\Hub\Editor\6000.0.0f1\Editor\Unity.exe"
) do (
  if exist %%V set "UNITY=%%~V"
)

if "%UNITY%"=="" (
  if exist "C:\Program Files\Unity\Hub\Editor" (
    for /f "delims=" %%D in ('dir /b /ad /o-n "C:\Program Files\Unity\Hub\Editor" 2^>nul') do (
      if exist "C:\Program Files\Unity\Hub\Editor\%%D\Editor\Unity.exe" (
        set "UNITY=C:\Program Files\Unity\Hub\Editor\%%D\Editor\Unity.exe"
        goto :found
      )
    )
  )
)

:found
if "%UNITY%"=="" (
  echo Unity.exe not found. Open the project in Unity Editor - map will auto-build on load.
  exit /b 1
)

echo Using: %UNITY%
echo Project: %PROJECT%

"%UNITY%" -batchmode -nographics -quit ^
  -projectPath "%PROJECT%" ^
  -executeMethod TheHeroBuildMainAssetsMapBatch.Run ^
  -logFile "%LOG%"

set ERR=%ERRORLEVEL%
type "%LOG%" 2>nul
exit /b %ERR%
