# Verifica build, testes e validate nos exemplos do repo Rust original.
param(
    [string]$ExamplesRoot = "E:\Projetos\hoblio\examples"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$Root = Split-Path -Parent $PSScriptRoot
$dotnet = "C:\Program Files\dotnet\dotnet.exe"

Push-Location $Root
try {
    & $dotnet build Hoblio.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw "build failed" }

    & $dotnet test Hoblio.sln -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw "tests failed" }

    $cli = Join-Path $Root "src\Hoblio.Cli\bin\Release\net8.0-windows\hoblio.exe"
    if (-not (Test-Path $cli)) { throw "CLI not found: $cli" }

    $demos = @(
        "demo-flow",
        "flow-control-demo",
        "variables-loops-demo",
        "files-lists-demo",
        "resilience-demo",
        "web-demo-flow",
        "browser-demo-flow"
    )

    $report = @()
    foreach ($name in $demos) {
        $dir = Join-Path $ExamplesRoot $name
        if (-not (Test-Path $dir)) {
            $report += "SKIP validate $name (missing)"
            continue
        }
        $code = cmd /c "`"$cli`" validate `"$dir`" >nul 2>&1 & exit /b %ERRORLEVEL%"
        if ($code -ne 0) {
            $report += "FAIL validate $name"
        } else {
            $report += "PASS validate $name"
        }
    }

    $browserProbe = Join-Path $ExamplesRoot "browser-demo-flow"
    if (Test-Path $browserProbe) {
        $env:HOBLIO_BROWSER_TEST = "1"
        & $dotnet test (Join-Path $Root "tests\Hoblio.Runtime.Tests\Hoblio.Runtime.Tests.csproj") -c Release --no-build --filter "FullyQualifiedName~Browser_navigate_local_html"
        if ($LASTEXITCODE -ne 0) {
            $report += "FAIL browser navigate (runtime test)"
        } else {
            $report += "PASS browser navigate (runtime test)"
        }
        Remove-Item Env:HOBLIO_BROWSER_TEST -ErrorAction SilentlyContinue
    }

    Write-Host ""
    Write-Host "=== Parity report ==="
    $report | ForEach-Object { Write-Host $_ }
    $fails = ($report | Where-Object { $_ -like "FAIL*" }).Count
    if ($fails -gt 0) { exit 1 }
}
finally {
    Pop-Location
}
