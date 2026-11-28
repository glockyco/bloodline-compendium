#!/usr/bin/env bash
# Runs the checks of .github/workflows on the working tree, in CI's order, so a push can be checked before CI sees it.
# Usage: nix develop -c scripts/ci-local.sh
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet csharpier check .
dotnet build --no-restore
dotnet test --no-build
bun run types:check
bun run guard
bun run --cwd site lint
bun run --cwd site format:check
bun run --cwd site check
bun run --cwd site test:unit
bun run --cwd site build:fixtures
bun run --cwd site test:e2e
