#!/usr/bin/env bash
set -euo pipefail

# ── cross compile: Linux host → Windows binary ────────────────

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'
say() { echo -e "${GREEN}[crossp]${NC} $*"; }
warn() { echo -e "${YELLOW}[crossp]${NC} $*"; }
die()  { echo -e "${RED}[crossp]${NC} $*"; exit 1; }

say "cross compile: Linux → Windows x64"

# ── 1. are we on linux? ───────────────────────────────────────
HOST_OS=$(uname -s)
if [ "$HOST_OS" != "Linux" ]; then
    die "this script is meant to run on Linux, not $HOST_OS.
    for macOS → Windows cross compile, just run: dotnet publish -r win-x64"
fi

# ── 2. check dotnet + wine (optional) ────────────────────────
say "checking dotnet sdk..."
command -v dotnet >/dev/null 2>&1 || die "dotnet sdk not found"

DOTNET_VER=$(dotnet --version 2>/dev/null || echo "???")
say "dotnet sdk: v${DOTNET_VER}"

# wine not required for build, but nice to have for quick smoke test
if command -v wine >/dev/null 2>&1; then
    WINE_VER=$(wine --version 2>/dev/null || echo "???")
    say "wine found: ${WINE_VER} (optional — for testing only)"
else
    warn "wine not found — can't locally test the output .exe"
    warn "  install: sudo apt install wine  (or your distro's pkg manager)"
fi

# ── 3. figure out paths ──────────────────────────────────────
PROJ_DIR="$(cd "$(dirname "$0")" && pwd)"
CSPROJ="$PROJ_DIR/SKIPPY.csproj"

if [ ! -f "$CSPROJ" ]; then
    die "SKIPPY.csproj not found at $CSPROJ"
fi

# ── 4. clean & restore ───────────────────────────────────────
say "cleaning old build artifacts..."
dotnet clean "$CSPROJ" -c Release -r win-x64 >/dev/null 2>&1 || true

say "restoring packages (including windows-native)..."
dotnet restore "$CSPROJ" -r win-x64 || die "restore failed.
    make sure 'System.Diagnostics.PerformanceCounter' package is available.
    The windows TFM dependencies need to be resolvable even on Linux."

# ── 5. cross-publish ─────────────────────────────────────────
# --self-contained bundles the full .net runtime + native libs.
# This means the output .exe will run on any Windows machine
# without needing .net installed.

WIN_RID="win-x64"
OUT_DIR="$PROJ_DIR/publish/${WIN_RID}"

say "cross-compiling for ${WIN_RID} (this may take a few minutes)..."
say "  dotnet will download windows runtime + native assets if needed"

dotnet publish "$CSPROJ" \
    -c Release \
    -r "$WIN_RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:DebugType=embedded \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$OUT_DIR" \
    || die "cross-compile failed.
    if the error mentions 'PerformanceCounter', try:
      dotnet add package System.Diagnostics.PerformanceCounter"

say "publish complete!"

# ── 6. copy skins ────────────────────────────────────────────
SKIN_SRC="$PROJ_DIR/皮肤"
SKIN_DST="$OUT_DIR/皮肤"

if [ -d "$SKIN_SRC" ]; then
    say "copying skins..."
    mkdir -p "$SKIN_DST"
    cp -r "$SKIN_SRC"/* "$SKIN_DST"/ 2>/dev/null || warn "no skin files?"
    skin_count=$(ls -1 "$SKIN_DST"/*.png 2>/dev/null | wc -l)
    say "  ${skin_count} skin(s) ready"
fi

# ── 7. show result ───────────────────────────────────────────
echo ""
EXE_PATH="$OUT_DIR/SKIPPY.exe"
if [ -f "$EXE_PATH" ]; then
    EXE_SIZE=$(ls -lh "$EXE_PATH" | awk '{print $5}')
    say "SUCCESS — output binary:"
    say "  ${EXE_PATH}"
    say "  size: ${EXE_SIZE}"
    echo ""
    say "copy the entire 'publish/${WIN_RID}' folder to a Windows machine and run SKIPPY.exe"

    # optional: quick wine smoke test
    if command -v wine >/dev/null 2>&1; then
        echo ""
        say "smoke test with wine? (y/n — wine may crash due to Avalonia/WPF rendering)"
        read -r -n 1 -t 5 answer 2>/dev/null || answer="n"
        echo ""
        if [ "$answer" = "y" ] || [ "$answer" = "Y" ]; then
            say "launching with wine (expect rendering errors on desktop pet, that's normal)..."
            cd "$OUT_DIR" && wine SKIPPY.exe || warn "wine crashed — this is expected for Avalonia GUI apps under wine.
    test on a real Windows machine instead."
        fi
    fi
else
    die "output binary not found at $EXE_PATH. build may have silently failed."
fi
