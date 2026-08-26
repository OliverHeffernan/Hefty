#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

"$SCRIPT_DIR/setup-linux.sh"
exec "$SCRIPT_DIR/dotnet-linux.sh" run --project Hefty.Sample.csproj --no-restore "$@"
