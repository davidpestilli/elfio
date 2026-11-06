<#
.SYNOPSIS
    Abre Chrome/Edge com CDP (--remote-debugging-port) num perfil dedicado, para um fluxo Elfio
    se anexar (browser_attach_*) a sessao que o humano esta usando.

.DESCRIPTION
    Pre-requisito do modo individual de um fluxo que assume uma tela ja aberta: um navegador
    iniciado "normalmente" NAO expoe porta de debug, e sem isso o browser_attach falha com
    "Could not attach to Chromium".

    Desde o Chrome/Edge 136 a flag --remote-debugging-port e ignorada no perfil padrao; por isso o
    script sempre aponta --user-data-dir para um diretorio proprio (padrao: %TEMP%\elfio-cdp-profile).
    Esse perfil e persistente: o login do eproc feito nele continua valendo na proxima execucao.

    Se ja existir um navegador rodando com esse mesmo perfil, o comando apenas abre mais uma aba nele
    (a porta de debug continua a mesma).

.PARAMETER Url
    Pagina aberta na sessao de debug. Padrao: about:blank.

.PARAMETER Engine
    chrome (padrao) ou edge.

.PARAMETER Port
    Porta CDP. Padrao: 9222 (a mesma que browser_attach_chrome/edge procura).

.PARAMETER ProfileDir
    Diretorio de dados do navegador. Padrao: %TEMP%\elfio-cdp-profile.

.PARAMETER Executable
    Caminho explicito do chrome.exe / msedge.exe (por padrao, procura nos locais usuais e usa
    HOBLIO_CHROME_PATH quando definido).

.PARAMETER NoWait
    Nao espera o endpoint CDP responder (apenas dispara o navegador).

.EXAMPLE
    pwsh scripts/start-cdp-browser.ps1 -Url https://eproc-1g-sp-hml.tjsp.jus.br/

.EXAMPLE
    pwsh scripts/start-cdp-browser.ps1 -Engine edge -Port 9223

.EXAMPLE
    # depois de logar no eproc, rodar o fluxo individual:
    dotnet run --project src/Hoblio.Cli -- run examples/eproc-peticionamento
#>
[CmdletBinding()]
param(
    [string]$Url = 'about:blank',
    [ValidateSet('chrome', 'edge')]
    [string]$Engine = 'chrome',
    [int]$Port = 9222,
    [string]$ProfileDir = (Join-Path $env:TEMP 'elfio-cdp-profile'),
    [string]$Executable,
    [switch]$NoWait
)

$ErrorActionPreference = 'Stop'

function Resolve-Browser {
    param([string]$Engine, [string]$Explicit)

    if ($Explicit) {
        if (-not (Test-Path -LiteralPath $Explicit)) { throw "executavel nao encontrado: $Explicit" }
        return (Resolve-Path -LiteralPath $Explicit).Path
    }
    if ($env:HOBLIO_CHROME_PATH) {
        if (-not (Test-Path -LiteralPath $env:HOBLIO_CHROME_PATH)) {
            throw "HOBLIO_CHROME_PATH aponta para arquivo inexistente: $env:HOBLIO_CHROME_PATH"
        }
        return (Resolve-Path -LiteralPath $env:HOBLIO_CHROME_PATH).Path
    }

    $rel = if ($Engine -eq 'edge') { 'Microsoft\Edge\Application\msedge.exe' } else { 'Google\Chrome\Application\chrome.exe' }
    $roots = @($env:ProgramFiles, ${env:ProgramFiles(x86)}, $env:LOCALAPPDATA) | Where-Object { $_ }
    foreach ($root in $roots) {
        $path = Join-Path $root $rel
        if (Test-Path -LiteralPath $path) { return $path }
    }

    throw "nao encontrei $rel. Instale o navegador, passe -Executable ou defina HOBLIO_CHROME_PATH."
}

function Get-CdpVersion {
    param($Client, [int]$Port)
    try {
        return $Client.GetStringAsync("http://127.0.0.1:$Port/json/version").GetAwaiter().GetResult()
    }
    catch {
        return $null
    }
}

function Get-CdpTabs {
    param($Client, [int]$Port)
    try {
        $json = $Client.GetStringAsync("http://127.0.0.1:$Port/json/list").GetAwaiter().GetResult()
        return @($json | ConvertFrom-Json | Where-Object { $_.type -eq 'page' })
    }
    catch {
        return @()
    }
}

$exe = Resolve-Browser -Engine $Engine -Explicit $Executable
if (-not (Test-Path -LiteralPath $ProfileDir)) {
    New-Item -ItemType Directory -Force -Path $ProfileDir | Out-Null
}

$browserArgs = @(
    "--remote-debugging-port=$Port",
    "--user-data-dir=$ProfileDir",
    '--remote-allow-origins=*',
    '--no-first-run',
    '--no-default-browser-check',
    ('"' + $Url + '"')
)

Write-Host "navegador : $exe"
Write-Host "perfil    : $ProfileDir"
Write-Host "CDP       : http://127.0.0.1:$Port"
Write-Host "pagina    : $Url"

$proc = Start-Process -FilePath $exe -ArgumentList $browserArgs -PassThru
Write-Host "processo  : $($proc.Id)"

if ($NoWait) { return }

$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.UseProxy = $false
$client = [System.Net.Http.HttpClient]::new($handler)
$client.Timeout = [TimeSpan]::FromSeconds(2)

try {
    $version = $null
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Milliseconds 250
        $version = Get-CdpVersion -Client $client -Port $Port
        if ($version) { break }
    }
    if (-not $version) {
        throw ("o navegador subiu (pid $($proc.Id)), mas nada respondeu em http://127.0.0.1:$Port. " +
               "Se ja havia um $Engine aberto com OUTRO perfil, ele pode ter capturado o pedido: feche-o e rode de novo.")
    }

    Write-Host 'CDP pronto.'
    $tabs = Get-CdpTabs -Client $client -Port $Port
    foreach ($tab in $tabs) {
        Write-Host ("  aba: {0}  [{1}]" -f $tab.title, $tab.url)
    }
    Write-Host ''
    Write-Host "Agora, no fluxo individual, o passo browser_attach_* encontra esta sessao na porta $Port."
    Write-Host 'Deixe esta janela aberta, navegue/logue no sistema e execute o fluxo.'
}
finally {
    $client.Dispose()
}
