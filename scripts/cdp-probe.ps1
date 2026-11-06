<#
.SYNOPSIS
    Probe read-only do DOM via CDP numa sessao de Chrome ja aberta (nao abre navegador).

.DESCRIPTION
    Conecta no endpoint de debugging (--remote-debugging-port) de um Chrome/Edge que o
    humano esta usando, avalia um JavaScript na aba escolhida e/ou salva um screenshot.

    Serve para CAPTURAR seletores reais de uma tela antes de montar o fluxo, em vez de
    inferi-los. Nao navega, nao clica, nao preenche: so le.

.EXAMPLE
    pwsh scripts/cdp-probe.ps1 -Js "document.title"

.EXAMPLE
    pwsh scripts/cdp-probe.ps1 -Js (Get-Content -Raw .\dump.js) -Out .\dump.json

.EXAMPLE
    pwsh scripts/cdp-probe.ps1 -Shot .\tela.png
#>
[CmdletBinding()]
param(
    # Expressao (ou corpo de funcao) JavaScript avaliado com returnByValue.
    [string]$Js,

    # Arquivo .js cujo conteudo e avaliado. Tem precedencia sobre -Js.
    [string]$JsFile,

    # Salva um PNG da viewport (ou da pagina inteira com -FullShot).
    [string]$Shot,

    # Captura a pagina inteira em vez da viewport.
    [switch]$FullShot,

    # Clique REAL (gesto de usuario) no primeiro elemento do seletor. Necessario para
    # botoes que abrem popup/janela (window.open bloqueado em clique sintetico).
    [string]$Click,

    # Digita texto com Input.insertText no elemento focado (ou no seletor informado).
    [string]$Type,

    # Texto a digitar com -Type.
    [string]$Text,

    # Endpoint CDP.
    [int]$Port = 9222,

    # Filtra a aba por substring de URL ou titulo. Padrao: primeira aba http(s).
    [string]$Match,

    # Mira um target exato por id (inclui about:blank e outras abas fora do filtro http).
    [string]$TargetId,

    # Chamada CDP crua: -Method "Page.bringToFront" -Params '{"accept":true}'.
    [string]$Method,

    # Parametros JSON da chamada crua.
    [string]$Params,

    # Mensagem de excecao vira erro (exit 1) em vez de ser impressa como resultado.
    [switch]$ThrowOnJsError,

    [int]$TimeoutMs = 20000
)

$ErrorActionPreference = 'Stop'

if ($JsFile) {
    if (-not (Test-Path -LiteralPath $JsFile)) { throw "JsFile nao encontrado: $JsFile" }
    $Js = Get-Content -LiteralPath $JsFile -Raw
}
if (-not $Js -and -not $Shot -and -not $Click -and -not $Type -and -not $Method) { throw 'informe -Js/-JsFile, -Shot, -Click, -Type ou -Method' }

function Get-Target {
    param([int]$Port, [string]$Match, [string]$TargetId)
    $list = Invoke-RestMethod -Uri "http://127.0.0.1:$Port/json/list" -TimeoutSec 5
    if ($TargetId) {
        $exact = @($list | Where-Object { $_.id -eq $TargetId })
        if ($exact.Count -eq 0) { throw "target id nao encontrado: $TargetId" }
        return $exact[0]
    }
    $pages = @($list | Where-Object { $_.type -eq 'page' -and $_.url -match '^https?://' })
    if ($pages.Count -eq 0) { throw "nenhuma aba http(s) no CDP :$Port (janela fechada?)" }
    if ($Match) {
        $hit = @($pages | Where-Object { $_.url -like "*$Match*" -or $_.title -like "*$Match*" })
        if ($hit.Count -eq 0) { throw "nenhuma aba casando '$Match'. Abas: $($pages.url -join ' | ')" }
        return $hit[0]
    }
    return $pages[0]
}

function Connect-Cdp {
    param([string]$WsUrl, [int]$TimeoutMs)
    $ws = [System.Net.WebSockets.ClientWebSocket]::new()
    $cts = [System.Threading.CancellationTokenSource]::new($TimeoutMs)
    try {
        $null = $ws.ConnectAsync([Uri]$WsUrl, $cts.Token).GetAwaiter().GetResult()
    }
    catch {
        $ws.Dispose()
        throw "falha ao conectar no CDP ($WsUrl): $($_.Exception.Message)"
    }
    return $ws
}

function Receive-CdpMessage {
    param($Ws, [int]$TimeoutMs)
    $buffer = New-Object byte[] 262144
    $ms = [System.IO.MemoryStream]::new()
    $ct = [System.Threading.CancellationTokenSource]::new($TimeoutMs)
    try {
        do {
            $seg = [ArraySegment[byte]]::new($buffer)
            $res = $Ws.ReceiveAsync($seg, $ct.Token).GetAwaiter().GetResult()
            if ($res.MessageType -eq [System.Net.WebSockets.WebSocketMessageType]::Close) { return $null }
            $ms.Write($buffer, 0, $res.Count)
        } while (-not $res.EndOfMessage)
    }
    finally { $ct.Dispose() }
    return [System.Text.Encoding]::UTF8.GetString($ms.ToArray())
}

$target = Get-Target -Port $Port -Match $Match -TargetId $TargetId
$ws = Connect-Cdp -WsUrl $target.webSocketDebuggerUrl -TimeoutMs $TimeoutMs
$script:msgId = 0

function Invoke-Cdp {
    param($Ws, [string]$Method, [hashtable]$Params = @{}, [int]$TimeoutMs = 20000)
    $script:msgId++
    $id = $script:msgId
    $payload = @{ id = $id; method = $Method; params = $Params } | ConvertTo-Json -Depth 12 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($payload)
    $seg = [ArraySegment[byte]]::new($bytes)
    $ct = [System.Threading.CancellationTokenSource]::new($TimeoutMs)
    try { $null = $Ws.SendAsync($seg, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $ct.Token).GetAwaiter().GetResult() }
    finally { $ct.Dispose() }

    $deadline = [datetime]::UtcNow.AddMilliseconds($TimeoutMs)
    while ($true) {
        $remaining = [int]([math]::Max(500, ($deadline - [datetime]::UtcNow).TotalMilliseconds))
        $raw = Receive-CdpMessage -Ws $Ws -TimeoutMs $remaining
        if ($null -eq $raw) { throw 'CDP fechou a conexao' }
        $msg = $raw | ConvertFrom-Json
        if ($msg.PSObject.Properties.Name -contains 'id' -and $msg.id -eq $id) { return $msg }
    }
}

$exit = 0
try {
    if ($Js) {
        $result = Invoke-Cdp -Ws $ws -Method 'Runtime.evaluate' -Params @{
            expression    = $Js
            returnByValue = $true
            awaitPromise  = $true
            userGesture   = $false
        } -TimeoutMs $TimeoutMs

        $exception = $result.result.exceptionDetails
        if ($exception) {
            $text = $exception.exception.description
            if (-not $text) { $text = $exception.text }
            if ($ThrowOnJsError) { throw "JS falhou: $text" }
            Write-Output "JS-ERROR: $text"
            $exit = 2
        }
        else {
            $value = $result.result.result.value
            if ($null -eq $value) {
                Write-Output "JS-RESULT: (undefined)"
            }
            elseif ($value -is [string]) {
                Write-Output $value
            }
            else {
                Write-Output ($value | ConvertTo-Json -Depth 12)
            }
        }
    }

    if ($Method) {
        $p = @{}
        if ($Params) {
            ($Params | ConvertFrom-Json).PSObject.Properties | ForEach-Object { $p[$_.Name] = $_.Value }
        }
        $raw = Invoke-Cdp -Ws $ws -Method $Method -Params $p -TimeoutMs $TimeoutMs
        if ($raw.error) { Write-Output ("CDP-ERROR: " + ($raw.error | ConvertTo-Json -Compress)); $exit = 3 }
        else { Write-Output ("CDP-OK: " + $Method + " " + ($raw.result | ConvertTo-Json -Compress -Depth 8)) }
    }

    if ($Click) {
        $selJson = ConvertTo-Json -InputObject $Click -Compress
        $box = Invoke-Cdp -Ws $ws -Method 'Runtime.evaluate' -Params @{
            expression    = "(function(){ const el = document.querySelector($selJson); if (!el) return null; el.scrollIntoView({block:'center'}); const r = el.getBoundingClientRect(); return {x: r.left + r.width/2, y: r.top + r.height/2, tag: el.tagName, txt: (el.innerText||'').trim().slice(0,40)}; })()"
            returnByValue = $true
        } -TimeoutMs $TimeoutMs
        $pt = $box.result.result.value
        if ($null -eq $pt) { throw "seletor nao encontrado: $Click" }
        foreach ($evt in @('mouseMoved', 'mousePressed', 'mouseReleased')) {
            $p = @{ type = $evt; x = $pt.x; y = $pt.y; button = 'left'; clickCount = 1; buttons = 1 }
            if ($evt -eq 'mouseMoved') { $p.buttons = 0 }
            $null = Invoke-Cdp -Ws $ws -Method 'Input.dispatchMouseEvent' -Params $p -TimeoutMs $TimeoutMs
        }
        Write-Output ("CLICK: {0} <{1}> '{2}' em ({3},{4})" -f $Click, $pt.tag, $pt.txt, [int]$pt.x, [int]$pt.y)
    }

    if ($Type) {
        $selJson = ConvertTo-Json -InputObject $Type -Compress
        $foco = Invoke-Cdp -Ws $ws -Method 'Runtime.evaluate' -Params @{
            expression    = "(function(){ const el = document.querySelector($selJson); if (!el) return null; if (el.focus) el.focus(); if (el.click && el.tagName !== 'BODY') el.click(); return {tag: el.tagName, id: el.id || '', ce: !!el.isContentEditable, valor: (el.value || el.innerText || '').slice(0,40)}; })()"
            returnByValue = $true
        } -TimeoutMs $TimeoutMs
        $f = $foco.result.result.value
        if ($null -eq $f) { throw "seletor nao encontrado: $Type" }
        $null = Invoke-Cdp -Ws $ws -Method 'Input.insertText' -Params @{ text = $Text } -TimeoutMs $TimeoutMs
        Write-Output ("TYPE: {0} <{1} id='{2}' contentEditable={3}> '{4}'" -f $Type, $f.tag, $f.id, $f.ce, $f.valor)
    }

    if ($Shot) {
        $null = Invoke-Cdp -Ws $ws -Method 'Page.enable'
        $shotParams = @{ format = 'png'; fromSurface = $true }
        if ($FullShot) { $shotParams.captureBeyondViewport = $true }
        $png = Invoke-Cdp -Ws $ws -Method 'Page.captureScreenshot' -Params $shotParams -TimeoutMs 30000
        $path = [System.IO.Path]::GetFullPath($Shot)
        $dir = [System.IO.Path]::GetDirectoryName($path)
        if ($dir -and -not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
        [System.IO.File]::WriteAllBytes($path, [System.Convert]::FromBase64String($png.result.data))
        Write-Output "SHOT: $path ($([math]::Round((Get-Item -LiteralPath $path).Length / 1kb, 1)) KB)"
    }
}
finally {
    try { $ws.Dispose() } catch { }
}

exit $exit
