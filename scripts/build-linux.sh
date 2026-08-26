#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

"$SCRIPT_DIR/setup-linux.sh"
exec "$SCRIPT_DIR/dotnet-linux.sh" build Hefty.sln --no-restore "$@"
