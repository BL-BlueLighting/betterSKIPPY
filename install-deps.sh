#!/usr/bin/env bash
set -euo pipefail

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'
say()  { echo -e "${GREEN}[deps]${NC} $*"; }
warn() { echo -e "${YELLOW}[deps]${NC} $*"; }
die()  { echo -e "${RED}[deps]${NC} $*"; exit 1; }

say "SKIPPY — install dependencies for screen monitor"
echo ""

# ── detect package manager ────────────────────────────────────
if command -v pacman >/dev/null 2>&1; then
    PM="pacman"
    say "detected: Arch Linux (pacman)"
elif command -v apt >/dev/null 2>&1; then
    PM="apt"
    say "detected: Debian/Ubuntu (apt)"
else
    die "could not detect pacman or apt.
    please install manually:
      - tesseract-ocr + tesseract-ocr-chi-sim (or tesseract-data-chi_sim)
      - maim (X11) or grim (Wayland) or imagemagick (import)"
fi

# ── install ───────────────────────────────────────────────────
case "$PM" in
    pacman)
        say "installing with pacman..."
        echo ""
        echo "  tesseract              — OCR engine"
        echo "  tesseract-data-chi_sim — Chinese (simplified) language data"
        echo "  maim                   — screenshot tool (X11)"
        echo "  grim                   — screenshot tool (Wayland)"
        echo ""

        sudo pacman -S --needed \
            tesseract \
            tesseract-data-chi_sim \
            maim \
            grim \
            imagemagick \
            || die "pacman install failed"
        ;;

    apt)
        say "installing with apt..."
        echo ""
        echo "  tesseract-ocr              — OCR engine"
        echo "  tesseract-ocr-chi-sim      — Chinese (simplified) language data"
        echo "  maim / grim / imagemagick  — screenshot tools"
        echo ""

        sudo apt update
        sudo apt install -y \
            tesseract-ocr \
            tesseract-ocr-chi-sim \
            maim \
            grim \
            imagemagick \
            || die "apt install failed"
        ;;
esac

# ── verify ────────────────────────────────────────────────────
echo ""
say "verifying..."
if command -v tesseract >/dev/null 2>&1; then
    say "  ✅ tesseract $(tesseract --version 2>&1 | head -1)"
else
    warn "  ⚠️  tesseract not found on PATH"
fi

for tool in maim grim import; do
    if command -v "$tool" >/dev/null 2>&1; then
        say "  ✅ $tool"
    fi
done

echo ""
say "done! you can now enable screen monitoring in SKIPPY settings."
