# Fluxo eproc: peticionamento (etapas 1–5, até a distribuição)

> Este documento é a **referência** (estrutura, seletores, etapas). O processo de investigação — hipóteses, medições, fracassos e o catálogo de armadilhas — está no [diário de construção](./diario-construcao-eproc.md).

## Objetivo

**Fluxo principal chamado** do superfluxo: ele **não navega nem faz login**. Assume a tela deixada por [`examples/login-eproc`](../../examples/login-eproc) — painel do advogado já logado — executa as 5 etapas do peticionamento, **distribui a petição** e devolve o número do processo em `PetitionNumber`.

```
eproc-superflow (orquestrador)
├─ run_flow project=login      → examples/login-eproc          (navega, loga, escolhe perfil)
└─ run_flow project=peticionar → examples/eproc-peticionamento (assume a tela, peticiona, distribui)
```

O peticionamento é dividido em **6 subfluxos** (ver § *Estrutura*): o `flow.json` principal só orquestra.

```
eproc-superflow (orquestrador)
├─ run_flow project=login      → examples/login-eproc          (navega, loga, escolhe perfil)
└─ run_flow project=peticionar → examples/eproc-peticionamento (assume a tela, peticiona)
```

Documento da composição (vínculos, `exports`/`requires`, semântica de herança): [composicao-superfluxos.md](./composicao-superfluxos.md).

| Pasta | Papel | `manifest` relevante |
|-------|-------|----------------------|
| [`examples/eproc-superflow`](../../examples/eproc-superflow) | orquestrador, declara os dois vínculos | `linked_projects` |
| [`examples/login-eproc`](../../examples/login-eproc) | chamado 1: login + perfil, termina no painel | `variables`: `ScreenText`, `EprocUrl`, `EprocUser` |
| [`examples/eproc-peticionamento`](../../examples/eproc-peticionamento) | chamado 2: este fluxo | `entry_flow: flow.json` |

## Contrato entre os fluxos

| Direção | Variável | Origem / destino |
|---------|----------|------------------|
| herda | `ScreenText` | rótulo da tela em que o login termina. Desde a correção do handoff é **pós-condição verificada** no `login-eproc` (`wait_panel` → `set_screen_panel`), não o último probe do `detect_screen`; o superfluxo roteia por ele, mas quem barra a cadeia é o `wait_menu` abaixo. Ver [composicao-superfluxos.md §9.4](./composicao-superfluxos.md). |
| herda | `EprocUrl`, `EprocUser` | ambiente/usuário escolhidos no login |
| local | `AutorNome` | nome do requerente pesquisado na etapa 3 |
| local | `ReuNome` | nome do requerido pesquisado na etapa 4 |
| entrega | `PetitionNumber` | **ainda não preenchido** — o número do protocolo só existe no fim da etapa 5. Enquanto isso o log do superfluxo mostra `protocolo` vazio; é esperado, não é bug |

Escopo: `inherit` (padrão). É o que faz o navegador ser o mesmo objeto e a tela ser "herdada" — ver [composicao-superfluxos.md](./composicao-superfluxos.md) §6.

## Etapas implementadas

Tudo abaixo foi **capturado no DOM real** (via `scripts/cdp-probe.ps1`) e conferido em execução manual na sessão do Chrome. Nenhum seletor é inferido, exceto os dois marcados com ⚠.

## Estrutura: um fluxo principal + 6 subfluxos

O projeto é dividido por etapa. O `flow.json` principal só orquestra; cada etapa vive em `subflows` (mesmo mecanismo do `detect_screen` do login: `run_flow` + `flow_id` + `share_browser: true` + `variable_scope: inherit`, então as variáveis são compartilhadas).

| Subfluxo | Nós | Entra em | Sai de | Etapa |
|---|---|---|---|---|
| `abrir_peticionamento` | 3 | `wait_menu` | `click_menu` | assume a tela deixada pelo login e abre o peticionamento |
| `etapa1_informacoes` | 13 | `wait_localidade_opcoes` | `wait_etapa2` | localidade, rito, área, classe, "Não se aplica", Próxima |
| `etapa2_assuntos` | 9 | `fill_filtro` | `wait_etapa3` | filtro, hover + Incluir Assunto, competência, Próxima |
| `etapa3_autor` | 10 | `wait_autor_form` | `wait_etapa4` | requerente: consulta por nome, Principal=Sim, incluir, Próxima |
| `etapa4_reu` | 11 | `wait_reu_form` | `wait_etapa5` | requerido: consulta, Qualificação=REQUERIDO, Principal=Sim, incluir, Próxima |
| `etapa5_documentos` | 21 | `shot_tela5` | `shot_final` | editor CKEditor, tipo/sigilo, anexo, Finalizar, iframe de confirmação, protocolo |

Fluxo principal (`flow.json`): `start → rf_abrir → rf_etapa1 → rf_etapa2 → rf_etapa3 → rf_etapa4 → rf_etapa5 → log_final → end` — 9 nós.

Como cada subfluxo começa com um `wait_for` da tela que ele espera (e termina com um `wait_content` que confirma a tela seguinte), rodar as etapas em sequência é a própria verificação: se uma etapa "passar" indevidamente, a seguinte falha imediatamente.

### Abrir o peticionamento (a partir do painel) — `abrir_peticionamento`

| # | Nó (`flow.json`) | Ação | Seletor / valor | Observação |
|---|------------------|------|-----------------|------------|
| — | `wait_menu` | `browser_wait_for` | `#main-menu a[href*="acao=processo_cadastrar"]`, `wait_visible`, 60 s | guarda do handoff: só existe no painel logado — é a guarda **observacional** real da cadeia (o link é "Petição Inicial") |
| — | `log_handoff` | `log` | — | registra tela/usuário herdados |
| — | `click_menu` | `browser_click` | mesmo seletor, `wait_selector_after: #selLocalidadeJudicial`, 60 s | abre o peticionamento |

### Etapa 1 — Informações do processo (`processo_cadastrar`) — `etapa1_informacoes`

| # | Nó | Ação | Seletor / valor | Observação |
|---|----|------|-----------------|------------|
| 1 | `sel_localidade` | `set_dropdown` | `#selLocalidadeJudicial` = `by_text` "São Paulo - Regional VIII - Tatuapé" | 368 opções **já vêm no HTML** (não há carregamento por clique); value `8` |
| — | `wait_rito_opcoes` | `browser_wait_for` | `#selRitoProcesso option[value="2"]`, 60 s | o `change` da localidade **esvazia o select sincronamente** e o AJAX repovoa (0,2–3,8 s). Esperar a opção voltar **é** o sinal exato — nenhum `delay` |
| 2 | `sel_rito` | `set_dropdown` | `#selRitoProcesso` = `by_text` "RITO ORDINÁRIO (COMUM)" | 3 opções; value `2` |
| — | `wait_area_rebuild` | `wait_content` | `not_contains_element` em `#selIdGrupoCompetencia option:nth-child(3)` | a Área **não** é esvaziada sincronamente: esperar ela encolher (6 → 2) é o que evita o rebuild engolir a seleção |
| — | `wait_area_opcoes` | `browser_wait_for` | `#selIdGrupoCompetencia option[value="0"]`, 60 s | garante a lista não vazia antes de setar |
| 3 | `sel_area` | `set_dropdown` | `#selIdGrupoCompetencia` = `by_text` "Cível" | value `0` |
| — | `wait_classe_ajax` | `browser_wait_for` | `#selIdClasseJudicial option:nth-child(2)`, 60 s | a Classe só carrega com localidade+rito+área (1 → 69 opções; medido 1,0–2,6 s) |
| 4 | `sel_classe` | `set_dropdown` | `#selIdClasseJudicial` = `by_text` "PETIÇÃO CÍVEL" | value `0000000158`, **índice 54** |
| — | `wait_pos_classe` | `browser_wait_for` | `#selTipoJustica`, 60 s | a tela pós-classe renderiza Tipo Justiça/Processo Originário/Juízo |
| 5 | `check_nao_aplica` | `set_checkbox` | `#chkNaoAplicaValor` = true | desabilita `#txtValorCausa` (handlers em **click**, e o motor usa `el.click()`) |
| 6 | `click_proxima` | `browser_click` | `#btnSalvar` (`name=sbmProcessoEtapa1`), `wait_for_navigation` | "Próxima >". Id **duplicado** no topo e no rodapé (mesmo submit); o clique pega o primeiro |
| — | `wait_etapa2` | `wait_content` | "2 de 5" em `#divInfraBarraLocalizacao`, 30 s | prova que a etapa 2 abriu |

Campos que a tela oferece e o fluxo **não** toca: `#selIdSigilo` (default "Sem Sigilo (Nível 0)"), `#selTipoJustica` (default "TJSP - PRIMEIRO GRAU"), `#txtProcessoOriginario`, `#txtDescJuizo` (`readOnly`, vem do servidor).

### Etapa 2 — Assuntos (`processo_cadastrar_2`)

| # | Nó | Ação | Seletor / valor | Observação |
|---|----|------|-----------------|------------|
| 7 | `fill_filtro` | `browser_fill` | `#txtFiltroPesquisa` = `02200307` | campo "Informe o assunto ou o código para busca" |
| 8 | `click_filtrar` | `browser_click` | `#btnFiltrar` | **existe botão** — não é preciso hack de Enter |
| — | `wait_assunto` | `browser_wait_for` | `[id='02200307_anchor']`, `wait_visible`, 60 s | árvore jstree; o filtro expande o caminho em ~0,5–1,6 s |
| 9 | `hover_assunto` | `browser_hover` | `[id='02200307_anchor']` | o botão da linha (`#imgIncluir02200307`) nasce `display:none` e **só aparece no hover** |
| 10 | `click_assunto` | `browser_click` | `#imgIncluir02200307` (`onclick="incluirAssunto('02200307')"`) | **precisa ser clique real**. Chamar `incluirAssunto()` por `browser_execute_script` atualiza só o DOM (linha e Competência) e **não** registra o assunto no formulário |
| — | `wait_assunto_efetivado` | `browser_execute_script` + retry 20×500 ms | `document.getElementById('hdnIdAssuntoPrincipal').value` | **guarda essencial**: o AJAX grava `hdnIdAssuntoPrincipal`/`hdnListaAssuntos` alguns instantes depois do clique. Sem esperar por ele, o "Próxima" envia a petição **sem assunto** |
| — | `wait_incluir` | `browser_wait_for` | `#selNumCodCompetencia`, 60 s | a Competência aparece junto (servidor já deixa "Cível", value `114`) |
| 11 | `click_etapa2` | `browser_click` | `#sbmProcessoEtapa2`, `wait_for_navigation` | "Próxima >" |

#### O sintoma que esse passo causou

Pular `hdnIdAssuntoPrincipal` não dá erro na hora: o "Próxima" funciona, as etapas 3–5 funcionam, e o estrago aparece só no fim — a página de confirmação cai num **erro PHP do eproc**:

```
Error: Call to a member function getStrDesAssunto() on null
/app/web/view/distribuicao/processo_confirmacao_finalizar_subfrm.php:491
```

No resumo, *"Assunto Principal:"* vem vazio. Como o iframe de confirmação fica com a página de erro, o `#sbmConfirmar` não existe e o fluxo falha com `Cannot read properties of null (reading 'click')` — dois sintomas, uma causa.

O botão "Incluir" do painel (`#btnIncluirAssunto` → `transportar()`) **não** faz parte deste caminho: ele serve para adicionar *outro* assunto e, sem seleção na árvore, apenas emite o alerta *"Escolha um assunto."*.
| — | `wait_etapa3` | `wait_content` | "3 de 5" | — |

### Etapa 3 — Partes (requerentes) (`processo_cadastrar_3_autor`)

| # | Nó | Ação | Seletor / valor | Observação |
|---|----|------|-----------------|------------|
| — | `wait_autor_form` | `browser_wait_for` | `#txtNome` | formulário "Consulta" carregado |
| 12 | `fill_autor` | `browser_fill` | `#txtNome` = `{AutorNome}` | pesquisa por nome |
| 13 | `click_consultar_autor` | `browser_click` | `#btnConsultarNome` | há também `#btnConsultar` (por CPF, `#txtCpfCnpj`) |
| — | `wait_autor_resultado` | `browser_wait_for` | `#tblResultadosBusca tr[id^='tr']`, 25 s | grade de resultados |
| 14 | `sel_autor_principal` | `set_dropdown` | `#tblResultadosBusca select[id^='selPrincipal']` = `by_text` "Sim" | `S`=Sim, `N`=Não |
| 15 | `click_incluir_autor` | `browser_click` | `#tblResultadosBusca button[id^='btnIncluir_']` | `incluirParte(...)` |
| — | `wait_autor_incluido` | `browser_wait_for` | `#tblPartes tr:nth-child(2)`, 25 s | primeira linha de dados da tabela de partes |
| 16 | `click_proxima_autor` | `browser_click` | `#btnProxima`, `wait_for_navigation` | — |
| — | `wait_etapa4` | `wait_content` | "4 de 5" | — |

### Etapa 4 — Partes (requeridos) (`processo_cadastrar_3_reu`)

Mesma estrutura da etapa 3, com **um select a mais** na linha do resultado:

| # | Nó | Ação | Seletor / valor | Observação |
|---|----|------|-----------------|------------|
| 17–19 | `wait_reu_form` / `fill_reu` / `click_consultar_reu` | idem etapa 3 | `#txtNome` = `{ReuNome}`, `#btnConsultarNome` | — |
| — | `wait_reu_resultado` | `browser_wait_for` | `#tblResultadosBusca tr[id^='tr']` | — |
| 20 | `sel_reu_qualificacao` | `set_dropdown` | `#tblResultadosBusca select[id^='selQualificacaoReu']` = `by_text` "REQUERIDO" | na etapa 4 há `selQualificacaoReu{N}` (REQUERIDO `58` / INTERESSADO `116`) além de `selPrincipal{N}` |
| 21 | `sel_reu_principal` | `set_dropdown` | `... select[id^='selPrincipal']` = "Sim" | — |
| 22 | `click_incluir_reu` | `browser_click` | `... button[id^='btnIncluir_']` | `dialog_action: accept` por segurança |
| — | `wait_reu_incluido` | `browser_wait_for` | `#tblPartes tbody tr` | 1ª linha de dados; o header está em `<thead>`, então `tr:nth-child(2)` **não** serve |
| 23 | `click_proxima_reu` | `browser_click` | `#btnProxima`, `wait_for_navigation` | — |
| — | `wait_etapa5` | `wait_content` | "5 de 5" | — |

### Etapa 5 — Documentos (`processo_cadastrar_4`) até a distribuição

Executada e confirmada de ponta a ponta: a petição foi **distribuída** e a tela final devolveu o número do processo.

| # | Nó | Ação | Seletor / valor | Observação |
|---|----|------|-----------------|------------|
| — | `wait_doc_form` | `browser_wait_for` | `#btnEnviarArquivos` | tela de documentos pronta |
| 24 | `click_digitar_doc` | `browser_click` | `button[onclick*=AbreEditorVaraNaoPiloto]` | "Digitar Documento" — botão **sem id**, abre um **popup** (nova página CDP) |
| 25 | `switch_editor` | `browser_switch_page` | `url: digitar_documento` | o motor passa a operar no popup; é preciso voltar depois |
| — | `wait_editor` | `browser_wait_for` | `iframe.cke_wysiwyg_frame` | CKEditor clássico (editor dentro de iframe) |
| 26 | `digitar_texto` | `browser_execute_script` | `CKEDITOR.instances.txt_fck.setData('<p>{DocTextoLatim}</p>')` | única forma prática: o corpo editável está **dentro** do iframe do editor |
| 27 | `conferir_texto` | `browser_execute_script` | `CKEDITOR.instances.txt_fck.getData().length` | evidência (238 chars) |
| 28 | `click_anexar_sair` | `browser_click` | `#cke_16` | "Anexar Documento e Sair" é botão da toolbar do CKEditor |
| — | `wait_dialogo_tipo` | `browser_wait_for` | `#JQueryUIselTipoArquivo` | diálogo jQuery UI "Tipo de documento" |
| 29 | `sel_tipo_documento` | `set_dropdown` | `#JQueryUIselTipoArquivo` = `by_text` "PETIÇÃO INICIAL" | 410 opções; o select é `display:none` (selectmenu), mas `value`+`change` funcionam |
| 30 | `sel_sigilo_documento` | `set_dropdown` | `#JQueryUIselIdSigilo` = `by_text` "Sem Sigilo (Nível 0)" | — |
| 31 | `click_ok_tipo` | `browser_click` | `.ui-dialog-buttonpane button.dialog-button` | primeiro botão = OK; o popup se fecha sozinho e o documento entra na tabela |
| — | `switch_principal` | `browser_switch_page` | `url: acao=processo_cadastrar_4&` | volta para a página do peticionamento |
| — | `wait_doc_anexado` | `browser_wait_for` | `#tbDocumentosCadastradas tbody tr`, 45 s | sinal preciso: a tabela tem `<tfoot>` (total), então `tbody tr` conta **só linhas de dados** |
| 35 | `click_finalizar` | `browser_click` | `#btnSalvar` (`name=sbmProcessoEtapa4`) | "Finalizar". `on_failure: continue` porque a navegação pode destruir o contexto do clique |
| 36 | `click_confirmar_ajuizamento` | `browser_execute_script` | `document.getElementById('ifrSubFrm').contentDocument.getElementById('sbmConfirmar').click()` | **a confirmação é um iframe** (`#ifrSubFrm` → `acao=processo_confirmacao_finalizar_su…`), fora do alcance de `browser_click`; `retry 10×1500 ms` espera o iframe carregar |
| — | `wait_distribuido` | `wait_content` | contains_text "Processo distribuído", 90 s | prova da distribuição |
| 37 | `capturar_protocolo` | `browser_get_text` | `#lblDesNumProcesso` → `PetitionNumber` | devolve o número CNJ (ex.: `4000922-41.2026.8.26.0008`) |

**O que NÃO se usa neste caminho:** `#btnEnviarArquivos` ("Confirmar seleção de documentos"). Ele valida o bloco de **upload** ("Documento 1" → `input[name='file']`), não o documento digitado; clicá-lo sem arquivo escolhido abre o alerta *"Documento 1 não informado."* e não faz nada. Pelo caminho do editor o documento já entra na tabela com `#hdnNumDocumentos = 1`.

**Lacuna de motor exposta aqui:** não há suporte a **iframe** nas ações `browser_*` (todas usam o documento principal). O passo da confirmação só funciona porque o iframe é same-origin e foi resolvido por `browser_execute_script`. Uma ação `browser_*_frame` (ou um parâmetro `frame:`) seria a evolução natural.

## Decisões que não são óbvias

### 1. `by_text` em vez de `by_index`/`by_value`

`by_index` lança `index out of range` quando a lista ainda não carregou, e o índice muda quando o eproc reordena as opções. `by_value` **falha em silêncio** (valor inexistente deixa o select em `-1` e o fluxo segue). `by_text` lança `option not found` — falha alto e o valor no `flow.json` é legível por um humano.

O modo `by_index` do motor é **0-based** (`el.selectedIndex = i`), enquanto `option:nth-child(N)` no CSS é 1-based. Fica o registro porque o histórico deste fluxo usava `by_index 55` para "Petição Cível" — e na lista real o índice 55 é **PETIÇÃO CÍVEL - CONFLITOS FUNDIÁRIOS**; a classe certa é a 54.

### 2. Espera entre dropdowns encadeados: o `change` esvazia o select

Escolher localidade reconstrói o Rito, o rito filtra a Área, a área carrega a Classe. A tentação é `delay`; foi o que este fluxo fazia, e era o principal custo (6 s + 2,5 s fixos, contra 0,2–2,6 s reais).

O que resolveu foi medir o comportamento: **o handler do `change` esvazia o select dependente de forma síncrona** e só depois dispara o AJAX que o repovoa. Consequência prática: `browser_wait_for "#selRitoProcesso option[value='2']"` **não pode passar cedo** (no instante seguinte ao `change` o select está com 0 opções) e retorna exatamente quando o rebuild terminou. Nada de tempo fixo.

Duas armadilhas descartadas no caminho:

- `select:empty` parece o sinal óbvio, mas não é: além de não casar quando sobra nó de texto, ele marca o **início** do rebuild, não o fim.
- Durante o rebuild a thread principal fica bloqueada (XHR síncrono do eproc): um poll de 20 ms só rodou depois de ~578 ms. Qualquer coisa que dependa de timer durante o AJAX "perde" o intervalo.

Resultado medido na etapa 1 inteira (sonda contra sessão viva): **~4 s no total**, dos quais ~2,6 s são a carga da Classe no servidor. Antes: ~31 s, sendo 20 s de sleep do `browser_attach` e 9 s de `delay`.

### 3. Filtro de assunto: botão, não Enter

`#btnFiltrar` (`acaoConsultar('filtrar')`) existe e resolve. O hack de `browser_execute_script` com `KeyboardEvent` saiu do fluxo — ficava caro e frágil. Fica a nota de motor: **não há ação de teclado** (`browser_press_button` é clique).

### 4. Scripts não podem conter `{` nem `}`

`RunVariables.Interpolate` varre chaves de forma ingênua: qualquer `{...}` é tratado como referência de variável e **lança** `invalid variable name` quando o conteúdo não é um identificador. Vale para todo `browser_execute_script` do projeto — este fluxo hoje não usa nenhum, mas a regra continua de pé.

### 5. Upload de arquivo exige ação própria

Não existe como preencher `input[type=file]` por JavaScript (o navegador proíbe por segurança). A ação nova **`browser_upload`** faz `DOM.setFileInputFiles` via `IElementHandle.UploadFileAsync`, o que funciona com uploaders JS como o FineUploader do eproc — o navegador dispara `input`/`change` como se o usuário tivesse escolhido o arquivo.

## Retentativas

As listas de Rito/Área/Classe recarregam por AJAX. Os nós de dropdown mantêm `retry_count` para absorver falha de rede, mas a espera correta é a dos nós `wait_*` — retry não substitui espera.

| Nó | `retry_count` | `retry_delay_ms` |
|----|---------------|------------------|
| `sel_rito` | 2 | 1200 |
| `sel_area` | 2 | 1200 |
| `sel_classe` | 2 | 1500 |

## Como executar

```powershell
# 1. validar contrato e grafo (sem rede)
dotnet run --project src/Hoblio.Cli -- validate examples/eproc-superflow

# 2. executar a cadeia (precisa de Chrome + credenciais do eproc)
dotnet run --project src/Hoblio.Cli -- run examples/eproc-superflow
```

Rodar `examples/eproc-peticionamento` **sozinho** falha de propósito em `wait_menu`: sem `browser_navigate`/attach não há sessão (`no browser session: run browser_navigate before other browser actions`). Este fluxo só existe dentro da cadeia.

## Como capturar os seletores de uma tela nova

```powershell
# Chrome visível com CDP (o fluxo do Studio/Runtime usa a mesma sessao)
chrome --remote-debugging-port=9222 --user-data-dir=$env:TEMP\elfio-cdp-profile https://eproc-1g-sp-hml.tjsp.jus.br/

# ler a aba (nao navega, nao clica)
pwsh scripts/cdp-probe.ps1 -Js "document.title"
pwsh scripts/cdp-probe.ps1 -JsFile scripts/cdp-dump-controls.js   # controles + seletores
pwsh scripts/cdp-probe.ps1 -Shot output/cdp/tela.png              # screenshot
```

## Testar uma etapa isolada, contra a sessão logada (sonda)

O ciclo "rodar a cadeia inteira" custa minutos (login + 4 telas). Para iterar numa tela só, existe o padrão-sonda: um projeto mínimo que **anexa** na aba já aberta e executa apenas os nós daquela etapa.

```powershell
# 1. Chrome visivel com CDP + voce logado e na tela desejada
# 2. projeto sonda: manifest.json + flow.json comeca com
#    { "type": "browser_attach_chrome", "attach_mode": "by_url",
#      "url": "acao=processo_cadastrar&", "debugging_port": 9222 }
dotnet run --project src/Hoblio.Cli -- run output/probe-etapa1
```

A sonda da etapa 1 (`output/probe-etapa1`) roda localidade → rito → área → classe contra a aba viva e imprime o estado final dos selects. É assim que os tempos de AJAX foram medidos e que a etapa é validada em ~10 s em vez de ~3 min.

## Verificação

Capturado e conferido em sessão real (HML, perfil Advogado), etapa por etapa e depois de ponta a ponta.

- **Etapa 1**: validada pela sonda contra sessão viva — estado final `Tatuapé | rito 2 | área 0 | classe 0000000158 | 69 opções | Tipo Justiça renderizou`. Tempos de AJAX medidos: Rito 0,6–3,8 s, Área ~1,0 s, Classe ~1,0 s.
- **Etapa 2**: validada na sonda e de ponta a ponta. O caminho correto é **hover na linha + clique real no botão "Incluir Assunto"** (`#imgIncluir02200307`) + espera de `hdnIdAssuntoPrincipal`; chamar `incluirAssunto()` por script atualiza só o DOM e **não** efetiva o assunto (ver o sintoma em § *O sintoma que esse passo causou*). O "Incluir" do painel (`transportar()`) não faz parte deste caminho.
- **Etapas 3–4**: executadas de ponta a ponta na cadeia (autor e réu incluídos, `#tblPartes tbody tr` como sinal).
- **Etapa 5**: executada e confirmada — petição **distribuída**, número devolvido em `#lblDesNumProcesso` (formato CNJ). Exigiu `browser_switch_page` (popup do editor), `browser_execute_script` no CKEditor e, na confirmação, um **iframe** (`#ifrSubFrm`) tratado por script — o motor não tem suporte a frame.
- **Cadeia completa (login → distribuição)**: rodada de ponta a ponta em **~32,7 s** (exit 0), com protocolo capturado em `PetitionNumber`. Três petições distribuídas durante a construção: `4000922-41`, `4000924-11` e `4000925-93` (todas `2026.8.26.0008`).
- **Números do fluxo**: 9 nós no principal + 6 subfluxos (79 nós) = **88 nós**, **zero `delay`**; timeouts de 45/60/90 s. Testes: 39 (modelo) + 55 (runtime) + 32 (Studio).
