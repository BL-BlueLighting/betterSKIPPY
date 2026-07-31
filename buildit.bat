@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul 2>&1

set "G=[92m" & set "R=[91m" & set "Y=[93m" & set "C=[96m" & set "N=[0m"

echo %G%[buildit]%N% SKIPPY build script

:: ── project dir ──────────────────────────────────────────────
set "PROJ_DIR=%~dp0"
set "CSPROJ=%PROJ_DIR%SKIPPY.csproj"

:: ── 0. check dotnet ──────────────────────────────────────────
echo %G%[buildit]%N% checking dotnet sdk...
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo %R%[buildit]%N% dotnet sdk not found!
    echo   install from: https://dotnet.microsoft.com/download
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do set "DOTNET_VER=%%i"
echo %G%[buildit]%N% dotnet sdk: v%DOTNET_VER%

:: detect arch
set "RID=win-x64"
if /i "%PROCESSOR_ARCHITECTURE%"=="AMD64"  set "RID=win-x64"
if /i "%PROCESSOR_ARCHITECTURE%"=="ARM64"  set "RID=win-arm64"
if /i "%PROCESSOR_ARCHITEW6432%"=="AMD64"  set "RID=win-x64"
if /i "%PROCESSOR_ARCHITEW6432%"=="ARM64"  set "RID=win-arm64"

echo %G%[buildit]%N% detected: windows
echo %G%[buildit]%N% target runtime: %RID%

:: ── 1. restore ───────────────────────────────────────────────
echo %G%[buildit]%N% restoring packages...
dotnet restore "%CSPROJ%"
if %errorlevel% neq 0 (
    echo %R%[buildit]%N% restore failed!
    exit /b 1
)

:: ── 3. build ─────────────────────────────────────────────────
set "OUT_DIR=%PROJ_DIR%publish\%RID%"

echo %G%[buildit]%N% building for %RID%...
dotnet publish "%CSPROJ%" ^
    -c Release ^
    -r %RID% ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:DebugType=embedded ^
    -o "%OUT_DIR%"
if %errorlevel% neq 0 (
    echo %R%[buildit]%N% build failed!
    exit /b 1
)

:: ── 4. copy skins ────────────────────────────────────────────
set "SKIN_SRC=%PROJ_DIR%皮肤"
set "SKIN_DST=%OUT_DIR%\皮肤"

if exist "%SKIN_SRC%" (
    echo %G%[buildit]%N% copying skins...
    if not exist "%SKIN_DST%" mkdir "%SKIN_DST%"
    xcopy /E /Y /Q "%SKIN_SRC%\*" "%SKIN_DST%\" >nul 2>&1
    echo %G%[buildit]%N%   skins copied
)

set "TESS_SRC=%PROJ_DIR%publish\tesseract"
set "TESS_DST=%OUT_DIR%\tesseract"
if exist "%TESS_SRC%" (
    echo %G%[buildit]%N% copying bundled tesseract...
    if not exist "%TESS_DST%" mkdir "%TESS_DST%"
    xcopy /E /Y /Q "%TESS_SRC%\*" "%TESS_DST%\" >nul 2>&1
    echo %G%[buildit]%N%   tesseract bundled
)

:: ── 5. result ────────────────────────────────────────────────
echo.
echo %G%[buildit]%N% build complete! → %OUT_DIR%
echo   run: %OUT_DIR%\SKIPPY.exe

:: ── 6. compress? ─────────────────────────────────────────────
echo.
set /p ANSWER="%G%[buildit]%N% compress output? (y/n): "
if /i not "%ANSWER%"=="y" (
    echo %G%[buildit]%N% skipping compression. done!
    goto :eof
)

:: find latest .zipkey (excluding Api.zipkey)
set "ZIPKEY_PATH="
set "LATEST_TIME=0"
for %%f in ("%PROJ_DIR%publish\*.zipkey") do (
    set "FN=%%~nxf"
    if /i not "!FN!"=="Api.zipkey" (
        set "FT=%%~tf"
        :: crude string compare for latest
        if exist "%%f" (
            for %%t in ("%%f") do (
                if /i "%%~tt" geq "!LATEST_TIME!" (
                    set "LATEST_TIME=%%~tt"
                    set "ZIPKEY_PATH=%%f"
                )
            )
        )
    )
)

if "%ZIPKEY_PATH%"=="" (
    echo %Y%[buildit]%N% no .zipkey found in publish\ (excluding Api.zipkey^)
    echo   create a publish\xxx.zipkey with the password on line 1.
    echo   ^(Api.zipkey is for api config, not zip encryption^)
    goto :eof
)

echo %G%[buildit]%N% using zipkey: %ZIPKEY_PATH%
set /p ZIP_PASS=<"%ZIPKEY_PATH%"

if "%ZIP_PASS%"=="" (
    echo %Y%[buildit]%N% zipkey empty — skipping compression
    goto :eof
)

set "ZIP_OUT=%PROJ_DIR%publish\%RID%.zip"

:: try 7z first, then fallback warning
where 7z >nul 2>&1
if %errorlevel% equ 0 (
    echo %G%[buildit]%N% compressing with 7z...
    7z a -p"%ZIP_PASS%" -mx=9 "%ZIP_OUT%" "%OUT_DIR%\*" >nul
    if %errorlevel% neq 0 (
        echo %R%[buildit]%N% 7z failed!
        exit /b 1
    )
) else (
    echo %Y%[buildit]%N% 7z not found — install 7-Zip for encrypted archives
    echo   https://www.7-zip.org/
    echo %Y%[buildit]%N% falling back to unencrypted zip via powershell...
    powershell -Command "Compress-Archive -Path '%OUT_DIR%\*' -DestinationPath '%ZIP_OUT%' -Force"
)

echo %G%[buildit]%N% archive created: %ZIP_OUT%
echo %G%[buildit]%N% all done!

endlocal
