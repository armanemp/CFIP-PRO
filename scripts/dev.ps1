$ErrorActionPreference = "Stop"

Write-Host "CFIP-PRO native Windows development checks"

if (-not (Get-Command uv -ErrorAction SilentlyContinue)) {
  throw "uv is required. Install it using the official uv installer."
}
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
  throw "npm/Node.js is required for the web workspace."
}

uv sync
uv run pytest
uv run ruff check .
uv run mypy apps/api/src

Push-Location apps/web
try {
  npm install
  npm run typecheck
  npm run build
} finally {
  Pop-Location
}
