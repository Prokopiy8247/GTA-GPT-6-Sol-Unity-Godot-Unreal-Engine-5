#!/bin/bash

set -u

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT="$SCRIPT_DIR/Unreal_GTA_GPT6Sol.uproject"
EDITOR=""

if [[ -n "${UE_ENGINE_ROOT:-}" ]] && [[ -d "$UE_ENGINE_ROOT/Engine/Binaries/Mac/UnrealEditor.app" ]]; then
  EDITOR="$UE_ENGINE_ROOT/Engine/Binaries/Mac/UnrealEditor.app"
fi

if [[ -z "$EDITOR" ]]; then
  shopt -s nullglob
  for candidate in /Users/Shared/Epic\ Games/UE_5.8*/Engine/Binaries/Mac/UnrealEditor.app; do
    if [[ -d "$candidate" ]]; then
      EDITOR="$candidate"
      break
    fi
  done
  shopt -u nullglob
fi

if [[ -z "$EDITOR" ]]; then
  echo
  echo "[ERROR] Unreal Engine 5.8 was not found."
  echo "Install UE 5.8 through Epic Games Launcher, then run this file again."
  echo "For a custom location, set UE_ENGINE_ROOT to the UE_5.8 folder."
  echo "See README.md for the one-time Xcode setup."
  echo
  read -r -p "Press Return to close..."
  exit 1
fi

echo "Starting Harbor City with:"
echo "$EDITOR"
if [[ "${1:-}" == "--check" ]]; then
  echo "Launcher check passed."
  exit 0
fi
open -n "$EDITOR" --args "$PROJECT" -game -log
