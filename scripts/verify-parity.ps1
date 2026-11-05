# Verifica build, testes e validate nos exemplos do repo Rust original.
param(
    [string]$ExamplesRoot = ""
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$Root = Split-Path -Parent $PSScriptRoot
$dotnet = "C:\Program Files\dotnet\dotnet.exe"

if ([string]::IsNullOrWhiteSpace($ExamplesRoot)) {
    $envRoot = $env:HOBLIO_EXAMPLES_ROOT
    if (-not [string]::IsNullOrWhiteSpace($envRoot) -and (Test-Path $envRoot)) {
        $ExamplesRoot = $envRoot
    }
    elseif (Test-Path (Join-Path $Root "examples\demo-flow")) {
        $ExamplesRoot = Join-Path $Root "examples"
    }
    elseif (Test-Path "E:\Projetos\hoblio\examples\demo-flow") {
        $ExamplesRoot = "E:\Projetos\hoblio\examples"
    }
    else {
        $ExamplesRoot = Join-Path $Root "examples"
    }
}

Write-Host "Examples root: $ExamplesRoot"

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
        "variables-demo-flow",
        "files-lists-demo",
        "resilience-demo",
        "flow-compose-subflow",
        "flow-compose-demo",
        "superflow-demo",
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
        $prevEap = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        & $cli validate $dir 1>$null 2>$null
        $code = $LASTEXITCODE
        $ErrorActionPreference = $prevEap
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
