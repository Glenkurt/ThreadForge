#!/usr/bin/env bash
set -euo pipefail

dotnet tool restore

dotnet ef database update \
  --project api/Api/Api.csproj \
  --startup-project api/Api/Api.csproj
