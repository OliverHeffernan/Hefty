#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

"$SCRIPT_DIR/dotnet-linux.sh" tool restore
"$SCRIPT_DIR/dotnet-linux.sh" restore Hefty.sln
