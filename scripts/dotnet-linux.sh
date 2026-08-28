#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/.." && pwd)"
DOTNET_VERSION="${HEFTY_DOTNET_VERSION:-10.0.400}"

cd "$REPO_ROOT"

if command -v mise >/dev/null 2>&1; then
  mise install "dotnet@$DOTNET_VERSION"
  DOTNET_ROOT="$(mise where "dotnet@$DOTNET_VERSION")"

  if [[ ! -x "$DOTNET_ROOT/dotnet" ]]; then
    echo "Error: mise installed .NET $DOTNET_VERSION but its dotnet executable was not found." >&2
    exit 1
  fi

  exec env \
    DOTNET_ROOT="$DOTNET_ROOT" \
    DOTNET_MULTILEVEL_LOOKUP=0 \
    PATH="$DOTNET_ROOT:$PATH" \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT="${DOTNET_SYSTEM_GLOBALIZATION_INVARIANT:-1}" \
    "$DOTNET_ROOT/dotnet" "$@"
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "Error: install mise or the .NET 10 SDK before running this script." >&2
  exit 1
fi

installed_version="$(
  DOTNET_SYSTEM_GLOBALIZATION_INVARIANT="${DOTNET_SYSTEM_GLOBALIZATION_INVARIANT:-1}" \
    dotnet --version 2>/dev/null || true
)"

case "$installed_version" in
  10.*) ;;
  *)
    echo "Error: this project requires the .NET 10 SDK; found ${installed_version:-no usable SDK}." >&2
    echo "Install mise or the .NET 10 SDK, then try again." >&2
    exit 1
    ;;
esac

exec env \
  DOTNET_SYSTEM_GLOBALIZATION_INVARIANT="${DOTNET_SYSTEM_GLOBALIZATION_INVARIANT:-1}" \
  dotnet "$@"
