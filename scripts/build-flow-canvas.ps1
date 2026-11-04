$ErrorActionPreference = "Stop"
$studioRoot = (Resolve-Path (Join-Path (Join-Path (Join-Path $PSScriptRoot "..") "src") "Hoblio.Studio")).Path
$canvasDir = Join-Path $studioRoot "flow-canvas"

function Invoke-CanvasBuild([string]$workingDir) {
  Push-Location $workingDir
  try {
    if (-not (Test-Path "node_modules")) { npm install }
    npm run build
  } finally {
    Pop-Location
  }
}

if ($studioRoot -match '#') {
  $tempRoot = Join-Path $env:TEMP "elfio-flow-canvas-build"
  if (Test-Path $tempRoot) { Remove-Item -Recurse -Force $tempRoot }
  Copy-Item -Recurse -Force $canvasDir $tempRoot
  try {
    Invoke-CanvasBuild $tempRoot
    $out = Join-Path $canvasDir "dist"
    if (Test-Path $out) { Remove-Item -Recurse -Force $out }
    Copy-Item -Recurse -Force (Join-Path $tempRoot "dist") $out
  } finally {
    Remove-Item -Recurse -Force $tempRoot -ErrorAction SilentlyContinue
  }
} else {
  Invoke-CanvasBuild $canvasDir
}
