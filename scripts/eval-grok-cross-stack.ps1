# Hoblio vs Hoblio C# — comparative evaluation (Grok bot / CI)
# Usage:
#   powershell -NoProfile -ExecutionPolicy Bypass -File scripts/eval-grok-cross-stack.ps1
#   powershell ... -File scripts/eval-grok-cross-stack.ps1 -RustRoot E:\Projetos\hoblio -CSharpRoot E:\Projetos\hoblio-c#
param(
    [string]$RustRoot = "",
    [string]$CSharpRoot = "",
    [string]$ExamplesRoot = "",
    [switch]$SkipBuild,
    [switch]$StrictSummaryLines
)

$ErrorActionPreference = "Continue"
$ProgressPreference = "SilentlyContinue"

function Resolve-DefaultRoots {
    param([string]$ScriptDir)
    $csharpCandidate = Split-Path -Parent $ScriptDir
    $rustCandidate = Join-Path (Split-Path -Parent $csharpCandidate) "hoblio"
    if ((Split-Path -Leaf $csharpCandidate) -eq "hoblio-c#") {
        return @{ CSharp = $csharpCandidate; Rust = $rustCandidate }
    }
    # Script lives under hoblio/scripts
    $rustCandidate = Split-Path -Parent $ScriptDir
    $csharpCandidate = Join-Path (Split-Path -Parent $rustCandidate) "hoblio-c#"
    return @{ Rust = $rustCandidate; CSharp = $csharpCandidate }
}

$defaults = Resolve-DefaultRoots $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($RustRoot)) { $RustRoot = $defaults.Rust }
if ([string]::IsNullOrWhiteSpace($CSharpRoot)) { $CSharpRoot = $defaults.CSharp }
if ([string]::IsNullOrWhiteSpace($ExamplesRoot)) { $ExamplesRoot = Join-Path $RustRoot "examples" }

# Avoid $env:TEMP on a full C: — use repo-local scratch (Grok/CI friendly).
$EvalTemp = Join-Path $RustRoot ".hoblio-eval-temp"
if (-not (Test-Path $EvalTemp)) {
    New-Item -ItemType Directory -Path $EvalTemp -Force | Out-Null
}

$Pass = 0
$Fail = 0

function Write-Eval {
    param(
        [string]$Id,
        [bool]$Ok,
        [string]$Detail = "",
        [switch]$Informational
    )
    if ($Informational) {
        $tag = if ($Ok) { "INFO PASS" } else { "INFO FAIL" }
        if ($Detail) { Write-Output "GROK_EVAL: $Id $tag $Detail" }
        else { Write-Output "GROK_EVAL: $Id $tag" }
        return
    }
    if ($Ok) {
        $script:Pass++
        if ($Detail) { Write-Output "GROK_EVAL: $Id PASS $Detail" }
        else { Write-Output "GROK_EVAL: $Id PASS" }
    } else {
        $script:Fail++
        if ($Detail) { Write-Output "GROK_EVAL: $Id FAIL $Detail" }
        else { Write-Output "GROK_EVAL: $Id FAIL" }
    }
}

function Write-Utf8NoBom {
    param([string]$Path, [string]$Content)
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $Content, $utf8)
}

function Invoke-External {
    param([Parameter(Mandatory = $true)][scriptblock]$Command)
    $global:LASTEXITCODE = $null
    $null = & $Command 2>&1
    $code = $global:LASTEXITCODE
    if ($null -eq $code) { return -1 }
    return [int]$code
}

function Test-CommandExists {
    param([string]$Name)
    $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Get-DotnetExe {
    $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $fallback = "C:\Program Files\dotnet\dotnet.exe"
    if (Test-Path $fallback) { return $fallback }
    return "dotnet"
}

function Invoke-RustCli {
    param([string]$Subcommand, [string]$ProjectPath)
    Push-Location $RustRoot
    try {
        $env:Path = "$env:USERPROFILE\.cargo\bin;" + $env:Path
        $global:LASTEXITCODE = $null
        $out = & cargo run -q -p hoblio-cli -- $Subcommand $ProjectPath 2>&1 | Out-String
        $code = $global:LASTEXITCODE
        if ($null -eq $code) { $code = -1 }
        return @{ Out = $out; Code = $code }
    } finally {
        Pop-Location
    }
}

function Invoke-CSharpCli {
    param(
        [string]$Subcommand,
        [string]$ProjectPath,
        [string]$CliExe
    )
    $global:LASTEXITCODE = $null
    $out = & $CliExe $Subcommand $ProjectPath 2>&1 | Out-String
    $code = $global:LASTEXITCODE
    if ($null -eq $code) { $code = -1 }
    return @{ Out = $out; Code = $code }
}

function Get-LastNodeId {
    param([string]$Text)
    if ($Text -match "last_node=(\S+)") { return $Matches[1].Trim() }
    return ""
}

function Get-ActionSummaries {
    param([string]$Text)
    $lines = $Text -split "`r?`n"
    $summaries = New-Object System.Collections.Generic.List[string]
    foreach ($line in $lines) {
        $t = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($t)) { continue }
        if ($t -match "^(valid:|run complete:)") { continue }
        if ($t -match "^(error|warning|Compiling|Finished|Running)") { continue }
        $summaries.Add($t)
    }
    return ($summaries | Sort-Object)
}

function Compare-RunOutputs {
    param(
        [hashtable]$Rust,
        [hashtable]$Cs,
        [string[]]$MustContain,
        [string[]]$MustNotContain = @(),
        [string]$ExpectedLastNode = "end"
    )
    $ok = $true
    $details = @()
    if ($Rust.Code -ne 0) { $ok = $false; $details += "rust exit $($Rust.Code)" }
    if ($Cs.Code -ne 0) { $ok = $false; $details += "csharp exit $($Cs.Code)" }
    $rustLast = Get-LastNodeId $Rust.Out
    $csLast = Get-LastNodeId $Cs.Out
    if ($rustLast -ne $ExpectedLastNode) { $ok = $false; $details += "rust last_node=$rustLast" }
    if ($csLast -ne $ExpectedLastNode) { $ok = $false; $details += "cs last_node=$csLast" }
    if ($rustLast -ne $csLast) { $ok = $false; $details += "last_node mismatch" }
    $mergedRust = $Rust.Out
    $mergedCs = $Cs.Out
    foreach ($needle in $MustContain) {
        $r = $mergedRust -match [regex]::Escape($needle)
        $c = $mergedCs -match [regex]::Escape($needle)
        if (-not $r) { $ok = $false; $details += "rust missing '$needle'" }
        if (-not $c) { $ok = $false; $details += "cs missing '$needle'" }
    }
    foreach ($needle in $MustNotContain) {
        if ($mergedRust -match [regex]::Escape($needle)) { $ok = $false; $details += "rust has '$needle'" }
        if ($mergedCs -match [regex]::Escape($needle)) { $ok = $false; $details += "cs has '$needle'" }
    }
    if ($StrictSummaryLines) {
        $rustSum = Get-ActionSummaries $mergedRust
        $csSum = Get-ActionSummaries $mergedCs
        if ($rustSum.Count -ne $csSum.Count) {
            $ok = $false
            $details += "summary count rust=$($rustSum.Count) cs=$($csSum.Count)"
        } else {
            for ($i = 0; $i -lt $rustSum.Count; $i++) {
                if ($rustSum[$i] -ne $csSum[$i]) {
                    $ok = $false
                    $details += "summary[$i] differs"
                    break
                }
            }
        }
    }
    return @{ Ok = $ok; Detail = ($details -join "; ") }
}

function New-TempProjectCopy {
    param(
        [string]$SourceDir,
        [string]$Suffix
    )
    $dest = Join-Path $EvalTemp $Suffix
    if (Test-Path $dest) { Remove-Item -Recurse -Force $dest -ErrorAction SilentlyContinue }
    Copy-Item $SourceDir $dest -Recurse -Force
    return $dest
}

# --- Prerequisites ---
Write-Eval "P1" (Test-CommandExists "rustc")
Write-Eval "P2" (Test-CommandExists "cargo")
Write-Eval "P3" (Test-CommandExists "dotnet")
Write-Eval "P4" (Test-Path $RustRoot) "rust root"
Write-Eval "P5" (Test-Path $CSharpRoot) "csharp root"
Write-Eval "P6" (Test-Path (Join-Path $ExamplesRoot "flow-control-demo\flow.json"))

if ($Fail -gt 0) {
    Write-Output "GROK_EVAL: SUMMARY pass=$Pass fail=$Fail blocked=prerequisites"
    exit 2
}

$dotnet = Get-DotnetExe
$csSln = Join-Path $CSharpRoot "Hoblio.sln"
$csCli = Join-Path $CSharpRoot "src\Hoblio.Cli\bin\Release\net8.0-windows\hoblio.exe"

if (-not $SkipBuild) {
    Push-Location $CSharpRoot
    $code = Invoke-External { & $dotnet build $csSln -c Release --nologo -v q }
    Pop-Location
    Write-Eval "T-B01" ($code -eq 0) $(if ($code -ne 0) { "dotnet build exit $code" })

    Push-Location $RustRoot
    $env:Path = "$env:USERPROFILE\.cargo\bin;" + $env:Path
    $code = Invoke-External { cargo build -q -p hoblio-cli }
    Pop-Location
    Write-Eval "T-B02" ($code -eq 0) $(if ($code -ne 0) { "cargo build exit $code" })
} else {
    Write-Eval "T-B01" (Test-Path $csCli) "skip build; need Release CLI"
    Write-Eval "T-B02" $true "skipped"
}

Push-Location $CSharpRoot
$testOut = & $dotnet test $csSln -c Release --no-build --nologo -v q 2>&1 | Out-String
$testCode = $global:LASTEXITCODE
if ($null -eq $testCode) { $testCode = -1 }
Pop-Location
Write-Eval "T-C01" ($testCode -eq 0) $(if ($testCode -ne 0) { "dotnet test exit $testCode" })
$approvedTotal = 0
foreach ($m in [regex]::Matches($testOut, "Aprovado:\s+(\d+)")) {
    $approvedTotal += [int]$m.Groups[1].Value
}
if ($approvedTotal -eq 0) {
    foreach ($m in [regex]::Matches($testOut, "Passed:\s+(\d+)")) {
        $approvedTotal += [int]$m.Groups[1].Value
    }
}
Write-Eval "T-C01-MIN" ($approvedTotal -ge 27) "approved=$approvedTotal (min 27)"

if (-not (Test-Path $csCli)) {
    Write-Eval "T-X01" $false "hoblio.exe not found at $csCli"
    Write-Output "GROK_EVAL: SUMMARY pass=$Pass fail=$Fail blocked=cli-missing"
    exit 2
}

# --- Validate parity (all examples with manifest) ---
$exampleDirs = Get-ChildItem $ExamplesRoot -Directory -ErrorAction SilentlyContinue
foreach ($dir in $exampleDirs) {
    $manifest = Join-Path $dir.FullName "manifest.json"
    if (-not (Test-Path $manifest)) { continue }
    $name = $dir.Name
    $path = $dir.FullName
    $rustV = Invoke-RustCli "validate" $path
    $csV = Invoke-CSharpCli "validate" $path $csCli
    Write-Eval "T-CMP-V-$name-R" ($rustV.Code -eq 0) $(if ($rustV.Code -ne 0) { "exit $($rustV.Code)" })
    Write-Eval "T-CMP-V-$name-C" ($csV.Code -eq 0) $(if ($csV.Code -ne 0) { "exit $($csV.Code)" })
    Write-Eval "T-CMP-V-$name-EQ" (($rustV.Code -eq 0) -and ($csV.Code -eq 0) -and ($rustV.Code -eq $csV.Code))
}

# --- Run parity: core demos (no browser/network) ---
$runDemos = @(
    @{
        Name = "flow-control-demo"
        Id   = "E01"
        Must = @("branch then", "item=alpha", "item=beta", "item=gamma", "loop finished")
        Not  = @("branch else")
    },
    @{
        Name = "variables-loops-demo"
        Id   = "E02"
        Must = @("while body counter=0", "done counter=4", "repeat index=3")
        Not  = @()
    },
    @{
        Name = "demo-flow"
        Id   = "E03"
        Must = @("log:")
        Not  = @()
    },
    @{
        Name = "resilience-demo"
        Id   = "E04"
        Must = @("failed (continued)", "continued after read failure")
        Not  = @()
    }
)

foreach ($demo in $runDemos) {
    $path = Join-Path $ExamplesRoot $demo.Name
    if (-not (Test-Path $path)) {
        Write-Eval "T-CMP-$($demo.Id)" $false "missing $path"
        continue
    }
    $rustR = Invoke-RustCli "run" $path
    $csR = Invoke-CSharpCli "run" $path $csCli
    $cmp = Compare-RunOutputs -Rust $rustR -Cs $csR -MustContain $demo.Must -MustNotContain $demo.Not
    Write-Eval "T-CMP-$($demo.Id)" $cmp.Ok $cmp.Detail
}

# --- files-lists-demo: run + compare report file ---
$filesDemo = Join-Path $ExamplesRoot "files-lists-demo"
if (Test-Path $filesDemo) {
    $reportPath = Join-Path $filesDemo "output\report.txt"
    if (Test-Path $reportPath) { Remove-Item $reportPath -Force -ErrorAction SilentlyContinue }
    $rustR = Invoke-RustCli "run" $filesDemo
    $rustReport = if (Test-Path $reportPath) { Get-Content $reportPath -Raw -Encoding UTF8 } else { "" }
    if (Test-Path $reportPath) { Remove-Item $reportPath -Force -ErrorAction SilentlyContinue }
    $csR = Invoke-CSharpCli "run" $filesDemo $csCli
    $csReport = if (Test-Path $reportPath) { Get-Content $reportPath -Raw -Encoding UTF8 } else { "" }
    $cmp = Compare-RunOutputs -Rust $rustR -Cs $csR -MustContain @("item=alpha", "item=beta") -MustNotContain @()
    Write-Eval "T-CMP-E05" $cmp.Ok $cmp.Detail
    $reportsMatch = ($rustReport.Length -gt 0) -and ($rustReport -eq $csReport)
    Write-Eval "T-CMP-E05-REPORT" $reportsMatch $(if (-not $reportsMatch) { "report.txt differs or missing" })
    if (Test-Path $reportPath) { Remove-Item $reportPath -Force -ErrorAction SilentlyContinue }
} else {
    Write-Eval "T-CMP-E05" $false "files-lists-demo missing"
}

# --- flag=no branch (both stacks) ---
$flagDemo = Join-Path $ExamplesRoot "flow-control-demo"
if (Test-Path $flagDemo) {
    $flagNoDir = New-TempProjectCopy $flagDemo "cross-eval-flag-no"
    $flowPath = Join-Path $flagNoDir "flow.json"
    if (-not (Test-Path $flowPath)) {
        Write-Eval "T-CMP-E06" $false "flow.json missing after copy"
    } else {
    $flagText = (Get-Content $flowPath -Raw -Encoding UTF8).Replace('"flag": "yes"', '"flag": "no"')
    Write-Utf8NoBom $flowPath $flagText
    $rustR = Invoke-RustCli "run" $flagNoDir
    $csR = Invoke-CSharpCli "run" $flagNoDir $csCli
    $cmp = Compare-RunOutputs -Rust $rustR -Cs $csR -MustContain @("branch else") -MustNotContain @("branch then")
    Write-Eval "T-CMP-E06" $cmp.Ok $cmp.Detail
    Remove-Item -Recurse -Force $flagNoDir -ErrorAction SilentlyContinue
    }
}

# --- Negative: duplicate main edge (validate must fail on both) ---
$dupDir = New-TempProjectCopy (Join-Path $ExamplesRoot "demo-flow") "cross-eval-dup-main"
$dupFlowPath = Join-Path $dupDir "flow.json"
$dupFlow = Get-Content $dupFlowPath -Raw -Encoding UTF8 | ConvertFrom-Json
$dupFlow.edges += [PSCustomObject]@{ from = "log_hello"; to = "end"; port = "main" }
Write-Utf8NoBom $dupFlowPath ($dupFlow | ConvertTo-Json -Depth 32)
$rustV = Invoke-RustCli "validate" $dupDir
$csV = Invoke-CSharpCli "validate" $dupDir $csCli
Write-Eval "T-CMP-N01-R" (($rustV.Code -ne 0) -and ($rustV.Out -match "duplicate outgoing port"))
Write-Eval "T-CMP-N01-C" (($csV.Code -ne 0) -and ($csV.Out -match "duplicate outgoing port"))
Write-Eval "T-CMP-N01-EQ" (($rustV.Code -ne 0) -and ($csV.Code -ne 0))
Remove-Item -Recurse -Force $dupDir -ErrorAction SilentlyContinue

# --- CLI contract (post-fix M1/M2/B1) ---
$demoFlow = Join-Path $ExamplesRoot "demo-flow"
if (Test-Path $demoFlow) {
    $jsonlDir = Join-Path $demoFlow ".hoblio"
    $jsonlPath = Join-Path $jsonlDir "run.jsonl"
    if (Test-Path $jsonlPath) { Remove-Item $jsonlPath -Force -ErrorAction SilentlyContinue }
    $global:LASTEXITCODE = $null
    $null = & $csCli run $demoFlow --jsonl 2>&1 | Out-String
    $runCode = $global:LASTEXITCODE
    $jsonOk = $false
    if ((Test-Path $jsonlPath) -and $runCode -eq 0) {
        $jsonBody = Get-Content $jsonlPath -Raw -ErrorAction SilentlyContinue
        $jsonOk = ($jsonBody -match '"kind"\s*:\s*"action_executed"') -and ($jsonBody -notmatch '\{\s*\}')
    }
    Write-Eval "T-CLI-J01" $jsonOk $(if (-not $jsonOk) { "run.jsonl missing action_executed or empty object" })

    $global:LASTEXITCODE = $null
    $validateErr = & $csCli validate $demoFlow 2>&1 | Out-String
    $validateCode = $global:LASTEXITCODE
    Write-Eval "T-CLI-V01" (($validateCode -eq 0) -and ($validateErr -match "valid:")) $(if ($validateCode -ne 0) { "exit $validateCode" })

    $negDir = New-TempProjectCopy $demoFlow "cross-eval-neg-increase"
    $negFlowPath = Join-Path $negDir "flow.json"
    $negMinimal = @'
{
  "start": "start",
  "variables": { "v": "abc" },
  "nodes": [
    {"id":"start","kind":"start","label":"Start"},
    {"id":"bad_inc","kind":"action","label":"bad","action":{"type":"increase_variable","variable":"v"}},
    {"id":"end","kind":"end","label":"End"}
  ],
  "edges": [
    {"from":"start","to":"bad_inc","port":"main"},
    {"from":"bad_inc","to":"end","port":"main"}
  ]
}
'@
    Write-Utf8NoBom $negFlowPath $negMinimal
    Remove-Item Env:HOBLIO_DEBUG -ErrorAction SilentlyContinue
    $global:LASTEXITCODE = $null
    $negOut = & $csCli run $negDir 2>&1 | Out-String
    $negCode = $global:LASTEXITCODE
    $negOk = ($negCode -ne 0) -and ($negOut -match "Error:") -and ($negOut -match "Caused by:") -and ($negOut -match "must be numeric")
    Write-Eval "T-CLI-E01" $negOk $(if (-not $negOk) { "exit=$negCode" })
    $negStrict = $negOk -and ($negOut -notmatch "Unhandled exception")
    Write-Eval "T-CLI-E01-STRICT" $negStrict $(if (-not $negStrict) { "System.CommandLine still prints Unhandled exception" }) -Informational
    Remove-Item -Recurse -Force $negDir -ErrorAction SilentlyContinue
} else {
    Write-Eval "T-CLI-J01" $false "demo-flow missing"
    Write-Eval "T-CLI-V01" $false "demo-flow missing"
    Write-Eval "T-CLI-E01" $false "demo-flow missing"
}

# --- Browser: unit test + cross-stack run (regression C1) ---
$runtimeTests = Join-Path $CSharpRoot "tests\Hoblio.Runtime.Tests\Hoblio.Runtime.Tests.csproj"
if (Test-Path $runtimeTests) {
    $env:HOBLIO_BROWSER_TEST = "1"
    Push-Location $CSharpRoot
    $brwOut = & $dotnet test $runtimeTests -c Release --no-build --nologo -v q --filter "FullyQualifiedName~Browser_navigate_local_html" 2>&1 | Out-String
    $brwCode = $global:LASTEXITCODE
    Pop-Location
    Remove-Item Env:HOBLIO_BROWSER_TEST -ErrorAction SilentlyContinue
    Write-Eval "T-BRW-UNIT" ($brwCode -eq 0) $(if ($brwCode -ne 0) { "browser unit test exit $brwCode" })
} else {
    Write-Eval "T-BRW-UNIT" $false "runtime tests project missing"
}

$browserDemo = Join-Path $ExamplesRoot "browser-demo-flow"
if (Test-Path $browserDemo) {
    $rustB = Invoke-RustCli "run" $browserDemo
    $csB = Invoke-CSharpCli "run" $browserDemo $csCli
    $bOk = ($rustB.Code -eq 0) -and ($csB.Code -eq 0)
    if ($bOk) {
        $merged = $rustB.Out + $csB.Out
        if ($merged -notmatch "example\.com|Example Domain|browser_navigate") {
            $bOk = $false
        }
    }
    $detail = @()
    if ($rustB.Code -ne 0) { $detail += "rust exit $($rustB.Code)" }
    if ($csB.Code -ne 0) { $detail += "cs exit $($csB.Code)" }
    Write-Eval "T-CMP-E07" $bOk ($detail -join "; ")
} else {
    Write-Eval "T-CMP-E07" $false "browser-demo-flow missing"
}

# --- Optional: validate-only for network/browser demos ---
foreach ($opt in @("web-demo-flow", "browser-demo-flow")) {
    $path = Join-Path $ExamplesRoot $opt
    if (-not (Test-Path $path)) { continue }
    $rustV = Invoke-RustCli "validate" $path
    $csV = Invoke-CSharpCli "validate" $path $csCli
    Write-Eval "T-CMP-OPT-$opt" (($rustV.Code -eq 0) -and ($csV.Code -eq 0))
}

Write-Output "GROK_EVAL: META rust_root=$RustRoot csharp_root=$CSharpRoot examples=$ExamplesRoot"
Write-Output "GROK_EVAL: SUMMARY pass=$Pass fail=$Fail"
if ($Fail -gt 0) { exit 1 }
exit 0
