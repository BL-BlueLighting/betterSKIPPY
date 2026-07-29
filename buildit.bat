@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul 2>&1

:: ── SKIPPY build script (Windows) ────────────────────────────

set "RED=[91m" & set "GREEN=[92m" & set "YELLOW=[93m" & set "NC=[0m"

echo %GREEN%[buildit]%NC% SKIPPY build script for Windows

:: ── 1. check dotnet ──────────────────────────────────────────
echo %GREEN%[buildit]%NC% checking dotnet sdk...

where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo %RED%[buildit]%NC% dotnet sdk not found!
    echo   install from: https://dotnet.microsoft.com/download
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do set "DOTNET_VER=%%i"
echo %GREEN%[buildit]%NC% dotnet sdk version: %DOTNET_VER%

:: ── 2. detect arch → runtime id ──────────────────────────────
echo %GREEN%[buildit]%NC% detected os: windows

set "RID=win-x64"
set "ARCH=unknown"

:: check PROCESSOR_ARCHITECTURE
if /i "%PROCESSOR_ARCHITECTURE%"=="AMD64"  set "ARCH=x64"
if /i "%PROCESSOR_ARCHITECTURE%"=="ARM64"  set "ARCH=arm64"

:: PROCESSOR_ARCHITEW6432 for 32-bit process on 64-bit os
if /i "%PROCESSOR_ARCHITEW6432%"=="AMD64"  set "ARCH=x64"
if /i "%PROCESSOR_ARCHITEW6432%"=="ARM64"  set "ARCH=arm64"

if /i "%ARCH%"=="x64"   set "RID=win-x64"
if /i "%ARCH%"=="arm64" set "RID=win-arm64"

echo %GREEN%[buildit]%NC% target runtime: %RID%

:: ── 3. build ─────────────────────────────────────────────────
set "PROJ_DIR=%~dp0"
echo %GREEN%[buildit]%NC% project dir: %PROJ_DIR%

echo %GREEN%[buildit]%NC% restoring packages...
dotnet restore "%PROJ_DIR%SKIPPY.csproj"
if %errorlevel% neq 0 (
    echo %RED%[buildit]%NC% restore failed!
    exit /b 1
)

echo %GREEN%[buildit]%NC% building for %RID%...
dotnet publish "%PROJ_DIR%SKIPPY.csproj" ^
    -c Release ^
    -r %RID% ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:DebugType=embedded ^
    -o "%PROJ_DIR%publish\%RID%"
if %errorlevel% neq 0 (
    echo %RED%[buildit]%NC% build failed!
    exit /b 1
)

:: ── 4. copy skins ────────────────────────────────────────────
set "SKIN_SRC=%PROJ_DIR%皮肤"
set "SKIN_DST=%PROJ_DIR%publish\%RID%\皮肤"

if exist "%SKIN_SRC%" (
    echo %GREEN%[buildit]%NC% copying skins...
    if not exist "%SKIN_DST%" mkdir "%SKIN_DST%"
    xcopy /E /Y /Q "%SKIN_SRC%\*" "%SKIN_DST%\" >nul 2>&1
)

:: ── 5. done ──────────────────────────────────────────────────
echo.
echo %GREEN%[buildit]%NC% build complete! ^(ﾟ▽^)ﾉ
echo   output: %PROJ_DIR%publish\%RID%\
echo.
echo   to run:  cd publish\%RID% ^&^& SKIPPY.exe
echo.

endlocal
