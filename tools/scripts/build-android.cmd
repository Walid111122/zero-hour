@echo off
REM ===========================================================================
REM  Headless Android build. Run with the Unity editor CLOSED -- batch mode
REM  needs the project lock.
REM
REM    tools\scripts\build-android.cmd              build the APK
REM    tools\scripts\build-android.cmd setinput     set Active Input Handling
REM
REM  Why cmd and not PowerShell: launching Unity through the PS call operator
REM  never actually started the process here -- $LASTEXITCODE came back empty
REM  after 0s with no log file, under both -ArgumentList arrays and backtick
REM  continuations. cmd passes a quoted command line straight through, which
REM  matters because the repo path contains spaces ("F:\last war build").
REM
REM  -quit exits 0 even when a build fails, so BatchBuild.BuildAndroid calls
REM  EditorApplication.Exit with an explicit code and this script forwards it.
REM ===========================================================================

setlocal

set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe"

REM Resolve the repo root to an absolute path. Passing "%~dp0..\..\client" through
REM unnormalized works for Unity but breaks findstr, which rejects ".." in a path.
for %%I in ("%~dp0..\..") do set "REPO=%%~fI"
set "PROJECT=%REPO%\client"


if /i "%~1"=="setinput" (
    set "METHOD=ZeroHour.EditorTools.PlayerSettingsSetup.ForceSingleInputHandling"
    set "LOGFILE=%REPO%\unity-setinput.log"
    set "LABEL=set input handler"
) else (
    set "METHOD=BatchBuild.BuildAndroid"
    set "LOGFILE=%REPO%\unity-android.log"
    set "LABEL=Android build"
)

if not exist "%UNITY%" (
    echo ERROR: Unity not found at "%UNITY%"
    exit /b 1
)

if exist "%LOGFILE%" del /f /q "%LOGFILE%"

echo === %LABEL% ===
echo   project : %PROJECT%
echo   method  : %METHOD%
echo   log     : %LOGFILE%
echo   started : %TIME%
echo.

"%UNITY%" -batchmode -quit -nographics -silent-crashes -projectPath "%PROJECT%" -executeMethod %METHOD% -logFile "%LOGFILE%"

set "CODE=%ERRORLEVEL%"

echo.
echo   exit    : %CODE%
echo   finished: %TIME%

if exist "%LOGFILE%" (
    echo.
    echo --- BatchBuild / PlayerSettings / errors ---
    REM A bare "Exception" pattern is useless here: it matches every frame of Unity's
    REM compilation stack traces and any package file with Exception in its name, which
    REM buries the four lines you actually want. BatchBuild prefixes its own failures
    REM with [BatchBuild] EXCEPTION, so the tag filters already cover them.
    findstr /c:"[BatchBuild]" /c:"[PlayerSettings]" /c:"error CS" /c:"Fatal Error" /c:"BuildFailedException" "%LOGFILE%"
) else (

    echo.
    echo WARNING: no log at "%LOGFILE%" -- Unity died before opening one.
)

exit /b %CODE%
