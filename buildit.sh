#!/usr/bin/env bash
set -euo pipefail

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'

say() { echo -e "${GREEN}[buildit]${NC} $*"; }
warn() { echo -e "${YELLOW}[buildit]${NC} $*"; }
die()  { echo -e "${RED}[buildit]${NC} $*"; exit 1; }

say "SKIPPY build script for Linux/macOS"

# check dotnet installed
say "checking dotnet sdk..."
command -v dotnet >/dev/null 2>&1 || die "dotnet sdk not found. install: https://dotnet.microsoft.com/download"
DOTNET_VER=$(dotnet --version 2>/dev/null || echo "???")
say "dotnet sdk version: ${DOTNET_VER}"

# detect os → runtime id
OS=$(uname -s)
ARCH=$(uname -m)

case "$OS" in
    Linux)
        say "detected os: linux"
        case "$ARCH" in
            x86_64|amd64)  RID="linux-x64"   ;;
            aarch64|arm64) RID="linux-arm64"  ;;
            *)
                RID="linux-x64"
                warn "unknown arch $ARCH, defaulting to linux-x64"
                ;;
        esac
        ;;
    Darwin)
        say "detected os: macos"
        case "$ARCH" in
            x86_64|amd64)  RID="osx-x64"    ;;
            arm64|aarch64) RID="osx-arm64"  ;;
            *)
                RID="osx-x64"
                warn "unknown arch $ARCH, defaulting to osx-x64"
                ;;
        esac
        ;;
    *)
        die "unsupported os: $OS. only linux / darwin(macos) supported."
        ;;
esac

say "target runtime: ${RID}"

# project dir = script dir
PROJ_DIR="$(cd "$(dirname "$0")" && pwd)"
say "project dir: ${PROJ_DIR}"

# restore packages
say "restoring packages..."
dotnet restore "$PROJ_DIR/SKIPPY.csproj" || die "restore failed"

# build & publish
say "building for ${RID}..."
dotnet publish "$PROJ_DIR/SKIPPY.csproj" \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:DebugType=embedded \
    -o "$PROJ_DIR/publish/${RID}" \
    || die "build failed"

# copy skins folder to output
SKIN_SRC="$PROJ_DIR/皮肤"
SKIN_DST="$PROJ_DIR/publish/${RID}/皮肤"
if [ -d "$SKIN_SRC" ]; then
    say "copying skins..."
    mkdir -p "$SKIN_DST"
    cp -r "$SKIN_SRC"/* "$SKIN_DST"/ 2>/dev/null || warn "no skin files to copy (maybe empty?)"
fi

# done — show output
say "build complete!"
say "output: ${PROJ_DIR}/publish/${RID}/"
ls -lh "$PROJ_DIR/publish/${RID}/SKIPPY" 2>/dev/null || \
ls -lh "$PROJ_DIR/publish/${RID}/SKIPPY.exe" 2>/dev/null || \
say "binary: $(find "$PROJ_DIR/publish/${RID}" -maxdepth 1 -type f -name 'SKIPPY*' | head -1)"

echo ""
say "to run:"
say "  cd publish/${RID} && ./SKIPPY"
