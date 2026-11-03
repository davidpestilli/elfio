# Hoblio C# — evaluation harness for flow-control slice (Grok bot / CI)
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/eval-flow-control.ps1
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
    $local = Join-Path $RepoRoot "examples\flow-control-demo\flow.json"
    if (Test-Path $local) { return Join-Path $RepoRoot "examples" }
    $sibling = "E:\Projetos\hoblio\examples\flow-control-demo\flow.json"
    if (Test-Path $sibling) { return "E:\Projetos\hoblio\examples" }
    return Join-Path $RepoRoot "examples"
}

function Get-CliPath {
    $release = Join-Path $RepoRoot "src\Hoblio.Cli\bin\Release\net8.0-windows\hoblio.exe"
    if (Test-Path $release) { return $release }
    return $null
}

function Run-Cli {
    param([string]$Subcommand, [string]$ProjectPath)
    $cli = Get-CliPath
    if (-not $cli) { return @{ Out = ""; Code = -1 } }
    $global:LASTEXITCODE = $null
    $out = & $cli $Subcommand $ProjectPath 2>&1 | Out-String
    $code = $global:LASTEXITCODE
    if ($null -eq $code) { $code = -1 }
    return @{ Out = $out; Code = $code }
}

$ExamplesRoot = Resolve-ExamplesRoot

Write-Eval "P1" (Test-Path $dotnet) "dotnet"
Write-Eval "P2" (Test-Path (Join-Path $ExamplesRoot "flow-control-demo\flow.json"))

if ($Fail -gt 0) {
    Write-Output "GROK_EVAL: SUMMARY pass=$Pass fail=$Fail blocked=prerequisites"
    exit 2
}

$code = Invoke-External { & $dotnet build Hoblio.sln -c Release }
Write-Eval "P3" ($code -eq 0) $(if ($code -ne 0) { "build exit $code" })
if ($code -ne 0) {
    Write-Output "GROK_EVAL: SUMMARY pass=$Pass fail=$Fail blocked=build"
    exit 2
}

$code = Invoke-External { & $dotnet test (Join-Path $RepoRoot "tests\Hoblio.Model.Tests\Hoblio.Model.Tests.csproj") -c Release --no-build }
Write-Eval "T-R01" ($code -eq 0) $(if ($code -ne 0) { "exit $code" })

$code = Invoke-External { & $dotnet test (Join-Path $RepoRoot "tests\Hoblio.Runtime.Tests\Hoblio.Runtime.Tests.csproj") -c Release --no-build }
Write-Eval "T-R02" ($code -eq 0) $(if ($code -ne 0) { "exit $code" })

$code = Invoke-External { & $dotnet test (Join-Path $RepoRoot "tests\Hoblio.Runtime.Tests\Hoblio.Runtime.Tests.csproj") -c Release --no-build --filter "FullyQualifiedName~UnitBehaviorTests" }
Write-Eval "T-R03" ($code -eq 0) $(if ($code -ne 0) { "variables unit exit $code" })

foreach ($dir in (Get-ChildItem $ExamplesRoot -Directory)) {
    $manifest = Join-Path $dir.FullName "manifest.json"
    if (-not (Test-Path $manifest)) { continue }
    $id = "T-V-" + $dir.Name
    $r = Run-Cli "validate" $dir.FullName
    Write-Eval $id ($r.Code -eq 0) $(if ($r.Code -ne 0) { "validate exit $($r.Code)" })
}

$demoPath = Join-Path $ExamplesRoot "flow-control-demo"
$r = Run-Cli "run" $demoPath
Write-Eval "T-E01" (($r.Code -eq 0) -and ($r.Out -match "last_node=end")) $(if ($r.Code -ne 0) { "run exit $($r.Code)" })
Write-Eval "T-E02" ($r.Out -match "branch then")
Write-Eval "T-E03" (
    ($r.Out -match "item=alpha") -and
    ($r.Out -match "item=beta") -and
    ($r.Out -match "item=gamma")
)
Write-Eval "T-E04" ($r.Out -match "loop finished")
Write-Eval "T-E05" (-not ($r.Out -match "branch else"))

$flagNoDir = Join-Path $env:TEMP "hoblio-eval-flag-no-csharp"
if (Test-Path $flagNoDir) { Remove-Item -Recurse -Force $flagNoDir }
Copy-Item $demoPath $flagNoDir -Recurse
$flowPath = Join-Path $flagNoDir "flow.json"
$flagText = (Get-Content $flowPath -Raw -Encoding UTF8).Replace('"flag": "yes"', '"flag": "no"')
Write-Utf8NoBom $flowPath $flagText
$rNo = Run-Cli "run" $flagNoDir
Write-Eval "T-E07" (
    ($rNo.Code -eq 0) -and
    ($rNo.Out -match "branch else") -and
    (-not ($rNo.Out -match "branch then"))
)

$code = Invoke-External {
    & $dotnet test (Join-Path $RepoRoot "tests\Hoblio.Model.Tests\Hoblio.Model.Tests.csproj") -c Release --no-build --filter "FullyQualifiedName~If_requires_then_and_else"
}
Write-Eval "T-N01" ($code -eq 0) $(if ($code -ne 0) { "exit $code" })

$n02Dir = Join-Path $env:TEMP "hoblio-eval-dup-main-csharp"
if (Test-Path $n02Dir) { Remove-Item -Recurse -Force $n02Dir }
Copy-Item (Join-Path $ExamplesRoot "demo-flow") $n02Dir -Recurse
$n02FlowPath = Join-Path $n02Dir "flow.json"
$n02Flow = Get-Content $n02FlowPath -Raw -Encoding UTF8 | ConvertFrom-Json
$n02Flow.edges += [PSCustomObject]@{ from = "log_hello"; to = "end"; port = "main" }
Write-Utf8NoBom $n02FlowPath ($n02Flow | ConvertTo-Json -Depth 32 -Compress:$false)
$n02 = Run-Cli "validate" $n02Dir
Write-Eval "T-N02" (($n02.Code -ne 0) -and ($n02.Out -match "duplicate outgoing port")) $(if ($n02.Code -eq 0) { "expected validation failure" })

$code = Invoke-External { & $dotnet build (Join-Path $RepoRoot "src\Hoblio.Studio\Hoblio.Studio.csproj") -c Release }
Write-Eval "T-F01" ($code -eq 0) $(if ($code -ne 0) { "studio build exit $code" })

Write-Output "GROK_EVAL: SUMMARY pass=$Pass fail=$Fail"
if ($Fail -gt 0) { exit 1 }
exit 0
