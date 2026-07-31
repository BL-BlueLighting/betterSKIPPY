@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul 2>&1

set "G=[92m" & set "R=[91m" & set "Y=[93m" & set "N=[0m"

echo %G%[deps]%N% SKIPPY — install dependencies for screen monitor (Windows)
echo.

:: ── check if tesseract already installed ─────────────────────
where tesseract >nul 2>&1
if %errorlevel% equ 0 (
    for /f "tokens=*" %%i in ('tesseract --version 2^>^&1 ^| findstr /i "tesseract"') do set "TESS_VER=%%i"
    echo %G%[deps]%N% tesseract already installed: !TESS_VER!
    echo %G%[deps]%N% done.
    goto :verify_chi_sim
)

:: ── download portable tesseract ──────────────────────────────
echo %G%[deps]%N% downloading tesseract portable for Windows...
echo.

set "TESS_URL=https://github.com/UB-Mannheim/tesseract/releases/download/v5.5.0/tesseract-ocr-w64-setup-5.5.0.20241111.exe"
set "TESS_EXE=%TEMP%\tesseract-installer.exe"
set "TESS_DIR=%~dp0publish\tesseract"

echo   url: %TESS_URL%
echo   dest: %TESS_DIR%
echo.

:: download
echo %G%[deps]%N% downloading (this may take a few minutes)...
powershell -Command "Invoke-WebRequest -Uri '%TESS_URL%' -OutFile '%TESS_EXE%'" 2>nul
if %errorlevel% neq 0 (
    echo %R%[deps]%N% download failed!
    echo   try downloading manually from:
    echo   https://github.com/UB-Mannheim/tesseract/releases
    echo   and install tesseract, then add it to PATH.
    goto :eof
)

:: ── extract / install ────────────────────────────────────────
echo %G%[deps]%N% installing tesseract...
if not exist "%TESS_DIR%" mkdir "%TESS_DIR%"

:: tesseract installer is an NSIS exe, we can extract with 7z if available
where 7z >nul 2>&1
if %errorlevel% equ 0 (
    echo   extracting with 7z...
    7z x "%TESS_EXE%" -o"%TESS_DIR%" -y >nul 2>&1
) else (
    echo   running installer silently...
    "%TESS_EXE%" /S /D="%TESS_DIR%"
)

:: clean up
del "%TESS_EXE%" 2>nul

echo %G%[deps]%N% tesseract installed to: %TESS_DIR%

:: ── verify chi_sim ────────────────────────────────────────────
:verify_chi_sim
set "TESSDATA="
:: check common locations
if exist "%TESS_DIR%\tessdata\chi_sim.traineddata" set "TESSDATA=%TESS_DIR%\tessdata"
if exist "C:\Program Files\Tesseract-OCR\tessdata\chi_sim.traineddata" set "TESSDATA=C:\Program Files\Tesseract-OCR\tessdata"

if not "%TESSDATA%"=="" (
    echo %G%[deps]%N% ✅ chi_sim language data found
) else (
    echo %Y%[deps]%N% ⚠️  chi_sim language data not found.
    echo   download from: https://github.com/tesseract-ocr/tessdata/raw/main/chi_sim.traineddata
    echo   and place in tessdata/ folder under tesseract install dir.
)

echo.
echo %G%[deps]%N% done.
echo   if tesseract is not on PATH, run:
echo     set PATH=%%PATH%%;%TESS_DIR%
echo   or add it in system environment variables.

endlocal
