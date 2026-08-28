#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/.." && pwd)"
PACKAGE_DIR="$REPO_ROOT/artifacts/packages"
ENGINE_PROJECT="$REPO_ROOT/Engine/Hefty.Engine.csproj"
SMOKE_PROJECT="$REPO_ROOT/tests/Hefty.PackageSmoke/Hefty.PackageSmoke.csproj"

if [[ -n "${DOTNET:-}" ]]; then
  DOTNET_COMMAND=("$DOTNET")
elif [[ "$(uname -s)" == "Linux" ]]; then
  DOTNET_COMMAND=("$SCRIPT_DIR/dotnet-linux.sh")
else
  DOTNET_COMMAND=(dotnet)
fi

rm -rf "$PACKAGE_DIR"
mkdir -p "$PACKAGE_DIR"

"${DOTNET_COMMAND[@]}" restore "$ENGINE_PROJECT"
"${DOTNET_COMMAND[@]}" pack "$ENGINE_PROJECT" --configuration Release --no-restore --output "$PACKAGE_DIR"

PACKAGE_VERSION="$("${DOTNET_COMMAND[@]}" msbuild "$ENGINE_PROJECT" -getProperty:PackageVersion -nologo)"
if [[ -z "$PACKAGE_VERSION" ]]; then
  echo "Error: could not determine the Hefty.Engine package version." >&2
  exit 1
fi

"${DOTNET_COMMAND[@]}" restore "$SMOKE_PROJECT" --no-cache -p:HeftyEngineVersion="$PACKAGE_VERSION"
"${DOTNET_COMMAND[@]}" build "$SMOKE_PROJECT" --configuration Release --no-restore -p:HeftyEngineVersion="$PACKAGE_VERSION"
