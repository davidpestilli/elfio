# Hoblio C# — evaluation harness for entregas A/B/C (slices 6–8)
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/eval-rpa-abc.ps1
$ErrorActionPreference = "Continue"

$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

$dotnet = "C:\Program Files\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }

function Write-Utf8NoBom {
    param([string]$Path, [string]$Content)
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $Content, $utf8)
}

$Pass = 0
$Fail = 0

function Write-Eval {
    param([string]$Id, [bool]$Ok, [string]$Detail = "")
    if ($Ok) {
        $script:Pass++
        if ($Detail) { Write-Output "GROK_EVAL: $Id PASS $Detail" } else { Write-Output "GROK_EVAL: $Id PASS" }
    } else {
        $script:Fail++
        if ($Detail) { Write-Output "GROK_EVAL: $Id FAIL $Detail" } else { Write-Output "GROK_EVAL: $Id FAIL" }
    }
}

function Invoke-External {
    param([Parameter(Mandatory = $true)][scriptblock]$Command)
    $global:LASTEXITCODE = $null
    $null = & $Command 2>&1
    $code = $global:LASTEXITCODE
    if ($null -eq $code) { return -1 }
    return [int]$code
}

function Resolve-ExamplesRoot {
    if ($env:HOBLIO_EXAMPLES_ROOT -and (Test-Path $env:HOBLIO_EXAMPLES_ROOT)) { return $env:HOBLIO_EXAMPLES_ROOT }
    $local = Join-Path $RepoRoot "examples\variables-loops-demo\flow.json"
    if (Test-Path $local) { return Join-Path $RepoRoot "examples" }
    $sibling = "E:\Projetos\hoblio\examples\variables-loops-demo\flow.json"
    if (Test-Path $sibling) { return "E:\Projetos\hoblio\examples" }
    return Join-Path $RepoRoot "examples"
}

function Get-CliPath {
    Join-Path $RepoRoot "src\Hoblio.Cli\bin\Release\net8.0-windows\hoblio.exe"
}

function Run-Cli {
    param([string]$Subcommand, [string]$ProjectPath)
    $cli = Get-CliPath
    $global:LASTEXITCODE = $null
    $out = & $cli $Subcommand $ProjectPath 2>&1 | Out-String
    $code = $global:LASTEXITCODE
    if ($null -eq $code) { $code = -1 }
    return @{ Out = $out; Code = $code }
}

$ExamplesRoot = Resolve-ExamplesRoot

Write-Eval "P1" (Test-Path $dotnet)
Write-Eval "P2" (Test-Path (Join-Path $ExamplesRoot "variables-loops-demo\flow.json"))
Write-Eval "P3" (Test-Path (Join-Path $ExamplesRoot "files-lists-demo\flow.json"))
Write-Eval "P4" (Test-Path (Join-Path $ExamplesRoot "resilience-demo\flow.json"))

if ($Fail -gt 0) {
    Write-Output "GROK_EVAL: SUMMARY pass=$Pass fail=$Fail blocked=prerequisites"
    exit 2
}

if (-not (Test-Path (Get-CliPath))) {
    $code = Invoke-External { & $dotnet build Hoblio.sln -c Release }
    Write-Eval "P5" ($code -eq 0) "release build for CLI"
    if ($code -ne 0) {
        Write-Output "GROK_EVAL: SUMMARY pass=$Pass fail=$Fail blocked=build"
        exit 2
    }
}

$code = Invoke-External { & $dotnet test (Join-Path $RepoRoot "tests\Hoblio.Model.Tests\Hoblio.Model.Tests.csproj") -c Release }
Write-Eval "T-R01" ($code -eq 0) $(if ($code -ne 0) { "exit $code" })

$code = Invoke-External { & $dotnet test (Join-Path $RepoRoot "tests\Hoblio.Runtime.Tests\Hoblio.Runtime.Tests.csproj") -c Release --filter "FullyQualifiedName~DemoFlowTests" }
Write-Eval "T-R02" ($code -eq 0) "demo integration tests"

$code = Invoke-External { & $dotnet test (Join-Path $RepoRoot "tests\Hoblio.Runtime.Tests\Hoblio.Runtime.Tests.csproj") -c Release --filter "FullyQualifiedName~UnitBehaviorTests" }
Write-Eval "T-R03" ($code -eq 0) "lists/adjust unit tests"

foreach ($name in @("variables-loops-demo", "files-lists-demo", "resilience-demo")) {
    $path = Join-Path $ExamplesRoot $name
    $r = Run-Cli "validate" $path
    Write-Eval "T-V-$name" ($r.Code -eq 0) $(if ($r.Code -ne 0) { "exit $($r.Code)" })
}

$loopsPath = Join-Path $ExamplesRoot "variables-loops-demo"
$r = Run-Cli "run" $loopsPath
Write-Eval "T-A01" (($r.Code -eq 0) -and ($r.Out -match "last_node=end"))
Write-Eval "T-A02" ($r.Out -match "while body counter=0")
Write-Eval "T-A03" ($r.Out -match "while body counter=3")
Write-Eval "T-A04" ($r.Out -match "done counter=4")
Write-Eval "T-A05" ($r.Out -match "repeat index=0")
Write-Eval "T-A06" ($r.Out -match "repeat index=3")

$filesPath = Join-Path $ExamplesRoot "files-lists-demo"
$r = Run-Cli "run" $filesPath
$reportPath = Join-Path $filesPath "output\report.txt"
$reportOk = $false
if (Test-Path $reportPath) {
    $reportText = Get-Content $reportPath -Raw -Encoding UTF8
    $reportOk = ($reportText -match "report") -and ($reportText -match "item=alpha") -and ($reportText -match "item=beta")
    Remove-Item $reportPath -Force -ErrorAction SilentlyContinue
}
Write-Eval "T-B01" (($r.Code -eq 0) -and ($r.Out -match "last_node=end"))
Write-Eval "T-B02" $reportOk "output/report.txt content"

$flowJson = Get-Content (Join-Path $filesPath "flow.json") -Raw -Encoding UTF8 | ConvertFrom-Json
$varProps = if ($null -eq $flowJson.variables) { @() } else { $flowJson.variables.PSObject.Properties }
Write-Eval "T-B03" ($varProps.Count -eq 0) "project variables empty; list built at run"

$resPath = Join-Path $ExamplesRoot "resilience-demo"
$r = Run-Cli "run" $resPath
Write-Eval "T-C01" (($r.Code -eq 0) -and ($r.Out -match "last_node=end"))
Write-Eval "T-C02" ($r.Out -match "failed \(continued\)") "continue summary on stdout"
Write-Eval "T-C03" ($r.Out -match "continued after read failure")

$stopDir = Join-Path $env:TEMP "hoblio-eval-stop-on-error-csharp"
if (Test-Path $stopDir) { Remove-Item -Recurse -Force $stopDir }
Copy-Item $resPath $stopDir -Recurse
$stopFlow = Join-Path $stopDir "flow.json"
$stopText = (Get-Content $stopFlow -Raw -Encoding UTF8).Replace('"on_failure": "continue"', '"on_failure": "stop"')
Write-Utf8NoBom $stopFlow $stopText
$rStop = Run-Cli "run" $stopDir
Write-Eval "T-C04" ($rStop.Code -ne 0) "run must fail when on_failure=stop"

$fcPath = Join-Path $ExamplesRoot "flow-control-demo"
$rFc = Run-Cli "run" $fcPath
Write-Eval "T-REG01" (($rFc.Code -eq 0) -and ($rFc.Out -match "item=beta"))

$code = Invoke-External { & $dotnet build (Join-Path $RepoRoot "src\Hoblio.Studio\Hoblio.Studio.csproj") -c Release }
Write-Eval "T-F01" ($code -eq 0) $(if ($code -ne 0) { "studio build exit $code" })

Write-Output "GROK_EVAL: SUMMARY pass=$Pass fail=$Fail"
if ($Fail -gt 0) { exit 1 }
exit 0
