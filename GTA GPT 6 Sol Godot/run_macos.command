#!/bin/sh
set -eu

PROJECT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)

if [ -d "$PROJECT_DIR/build/macos/Harborline.app" ]; then
  open "$PROJECT_DIR/build/macos/Harborline.app"
  exit 0
fi

if [ -x "/Applications/Godot.app/Contents/MacOS/Godot" ]; then
  exec "/Applications/Godot.app/Contents/MacOS/Godot" --path "$PROJECT_DIR"
fi

if command -v godot >/dev/null 2>&1; then
  exec godot --path "$PROJECT_DIR"
fi

if command -v godot4 >/dev/null 2>&1; then
  exec godot4 --path "$PROJECT_DIR"
fi

printf '%s\n' \
  'Godot 4.7.2 was not found.' \
  'Install it from https://godotengine.org/download/archive/4.7.2-stable/' \
  "or download the ready macOS build from this repository's Releases page."
printf 'Press Return to close...'
read -r _answer
exit 1
