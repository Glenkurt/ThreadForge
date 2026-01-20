#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <MigrationName>"
  exit 1
fi

dotnet tool restore

dotnet ef migrations add "$1" \
  --project api/Api/Api.csproj \
  --startup-project api/Api/Api.csproj
