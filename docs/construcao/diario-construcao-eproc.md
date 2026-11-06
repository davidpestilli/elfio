# Diário de construção — peticionamento no eproc

Registro do **processo**: o que foi investigado, quais hipóteses foram levantadas, como cada uma foi testada, quais números saíram e o que cada fracasso ensinou. Serve como memória técnica e como runbook para a próxima automação de tela no eproc (a de intimação, com perfil de Chefe de Cartório).

- **Referência de seletores e estrutura:** [fluxo-eproc-peticionamento.md](./fluxo-eproc-peticionamento.md)
- **Composição/superfluxo:** [composicao-superfluxos.md](./composicao-superfluxos.md)
- **Armadilhas genéricas de motor:** [guia-construcao.md](../guia-construcao.md)

## 0. Resultado em uma linha

Três projetos (`login-eproc` → `eproc-peticionamento` com 6 subfluxos → `eproc-superflow`), **88 nós, zero `delay`**, cadeia completa (login SSO → distribuição → protocolo) em **~32 s**, validada com **3 petições realmente distribuídas** em HML.

## 1. Objetivo e escopo

| Item | Definição |
|---|---|
| Alvo | eproc 1G SP, **HML** (`eproc-1g-sp-hml.tjsp.jus.br`) |
| Conta | `WS-DSDP` (SISTEMA DE PROCURADORIA EXTERNO) — conta **multi-perfil** |
| Perfil usado | Advogado (o perfil de Chefe de Cartório fica para o fluxo de intimação) |
| Objetivo | da tela de login até a **distribuição da petição**, capturando o número do processo |
| Escopo do documento | as 5 telas do peticionamento + login + a composição em superfluxo |

## 2. Método

Três regras que se pagaram ao longo de todo o trabalho:

1. **Nenhum seletor inferido.** Todo seletor foi lido do DOM vivo. Vários "seletores óbvios" estavam errados por um detalhe de marcação (ver §6).
2. **Nenhum tempo fixo.** Toda espera é um **sinal do DOM**. `delay` só entra quando o DOM não oferece sinal — e nesse caso o valor vem de medição, não de palpite.
3. **Hipótese tem que custar segundos.** Para isso existe a **sonda**: um projeto mínimo que anexa na aba já aberta e roda só uma etapa.

### 2.1 Ferramentas

| Ferramenta | Para quê |
|---|---|
| `scripts/cdp-probe.ps1` | lê/interage com a aba por CDP: `-Js`, `-JsFile`, `-Shot`, `-Click` (gesto real), `-Type`, `-Match`, `-TargetId`, `-Method`/`-Params` (CDP cru) |
| `scripts/cdp-dump-controls.js` | dump estruturado de inputs/selects/botões com seletores |
| `scripts/start-cdp-browser.ps1` | abre Chrome/Edge **com** CDP num perfil dedicado (`%TEMP%\elfio-cdp-profile`) e espera o endpoint responder — é o pré-requisito do modo individual do peticionamento. Equivalentes: **Studio → Navegador → Abrir CDP…** e `dotnet run --project src/Hoblio.Cli -- browser --port 9222` |
| Chrome + `--remote-debugging-port=9222` | navegador visível para o humano navegar/logar; o agente lê pela mesma sessão |
| timestamps no CLI | `dotnet run ... \| ForEach-Object { "{0,6:N2}s {1}" -f $sw.Elapsed.TotalSeconds, $_ }` → tempo por ação |
| `FormData` da página | para saber **o que o formulário realmente envia** (foi o que resolveu o caso do assunto) |
| Sondas (`output/probe-etapa1`, `-etapa34`, `-tela5`, `-finalizar`, `-treino2/3`) | rodar uma etapa contra a sessão viva, em ~5–30 s |

### 2.2 Ciclo de trabalho

```
humano navega  →  agente captura (probe)  →  agente mede  →  ajusta o nó  →  sonda  →  cadeia completa
```

A sonda é o que torna o ciclo curto: um run completo custa ~35 s e **distribui uma petição**; uma sonda custa 5–10 s e não distribui nada.

## 3. Arquitetura final

```
examples/
  login-eproc/            → login:entry   (SSO + perfil + handoff para o painel)
  eproc-peticionamento/   → entry: flow.json (orquestra 6 subfluxos)
  eproc-superflow/        → run_flow login → run_flow peticionar
```

Fluxo principal do `eproc-peticionamento` (9 nós):

```
start → rf_abrir → rf_etapa1 → rf_etapa2 → rf_etapa3 → rf_etapa4 → rf_etapa5 → log_final → end
```

| Subfluxo | Nós | Entra em | Sai de | Papel |
|---|---|---|---|---|
| `abrir_peticionamento` | 3 | `wait_menu` | `click_menu` | assume a tela do login e abre o peticionamento |
| `etapa1_informacoes` | 13 | `wait_localidade_opcoes` | `wait_etapa2` | localidade → rito → área → classe → "Não se aplica" → Próxima |
| `etapa2_assuntos` | 9 | `fill_filtro` | `wait_etapa3` | filtro → hover + Incluir Assunto → competência → Próxima |
| `etapa3_autor` | 10 | `wait_autor_form` | `wait_etapa4` | requerente |
| `etapa4_reu` | 11 | `wait_reu_form` | `wait_etapa5` | requerido |
| `etapa5_documentos` | 21 | `shot_tela5` | `shot_final` | CKEditor → tipo/sigilo → anexo → Finalizar → iframe → protocolo |

Semântica usada: `run_flow` com `flow_id` + `share_browser: true` + **`variable_scope: inherit`** — o mesmo dicionário de variáveis do run (`AutorNome`, `ReuNome`, `PeticaoDocTipo`, `DocTextoLen`, `PetitionNumber`, `EprocFinal`, `ScreenText`…), sem fio entre os passos. `outputs` só afeta a linha de resumo do log.

Propriedade útil do desenho: **cada subfluxo começa esperando a tela que ele resolve** e **termina confirmando a tela seguinte** (`wait_content` "N de 5"). Se uma etapa "passar" sem fazer o trabalho, a próxima falha imediatamente.

## 4. Investigações

Cada item segue Sintoma → Hipóteses → Experimento → Números → Conclusão.

### 4.1 "superfluxo eproc abortado: tela inesperada `Seleção de perfil`"

**Sintoma.** O login chegava ao Painel do Advogado, o log dizia "painel pronto", e o superfluxo abortava no `switch {ScreenText}`.

**Hipóteses.** (a) o login parava antes do painel; (b) `ScreenText` estava velho; (c) o `switch` do superfluxo estava errado.

**Experimento.** Leitura do log + do código: `rf_login` não declara `variable_scope` → escopo `inherit` (padrão) → o `ScreenText` do filho **é** o do pai (`FlowExecutor.cs`). O último `detect_screen` rodava *antes* do clique em `#tr1`; depois do clique havia só um `log` com texto fixo.

**Números.** O resumo já imprimia o defeito: `run_flow login:entry finished at end (ScreenText=Seleção de perfil, …)`.

**Conclusão.** `ScreenText` era um **cache do último probe**, não a tela atual. Agravante: o terceiro probe (`#divInfraBarraLocalizacao > div > h1`) é o título de **qualquer** página eproc — catch-all que sempre deixa um valor plausível, nunca um erro.

**Correção.** Pós-condição verificada no login: `wait_panel` (`#main-menu a[href*="acao=processo_cadastrar"]`) → `set_screen_panel` (`ScreenText = "Painel do Advogado"`) → `log` interpolando `{ScreenText}`. No superfluxo, o `case_default` deixou de abortar (virou aviso). Registrado em [composicao-superfluxos.md §9.4](./composicao-superfluxos.md).

### 4.2 "Evaluation failed: index out of range at `__puppeteer_evaluation_script__`:5:81"

**Sintoma.** Falha ao escolher a Classe Processual.

**Hipóteses.** (a) seletor errado; (b) lista ainda não carregada; (c) índice fora da faixa.

**Experimento.** O erro é literalmente o `throw new Error('index out of range')` de `BrowserContext.SetDropdown` quando `i >= el.options.length`. Captura do DOM: `#selIdClasseJudicial` tinha **1 opção** (`value="null"`) no momento do erro.

**Números.** A lista de classes só existe depois de localidade+rito+área: 1 → **69 opções**, `PETIÇÃO CÍVEL` no **índice 54**. O fluxo usava `by_index 55` → no índice 55 está *PETIÇÃO CÍVEL - CONFLITOS FUNDIÁRIOS* (classe **errada**, mesmo se tivesse funcionado).

**Conclusão.** Três defeitos encadeados: falta de espera pelo AJAX, off-by-one na classe, e o modo `by_index` (que explode) combinado com `by_value` (que **falha em silêncio**: `el.value` inexistente deixa o select em `-1` e o fluxo segue).

**Correção.** Tudo para `by_text` (falha alto: `option not found`) + esperas por sinal (§4.4).

### 4.3 "A tela 1 leva 20 segundos"

**Sintoma.** Entrar e sair da tela 1 custava ~20 s.

**Hipóteses.** (a) as 368 localidades demoram a carregar; (b) é preciso clicar na caixa para carregar; (c) algo no motor.

**Experimentos e números.**

| Medição | Resultado |
|---|---|
| carregar a página (TTFB / DOM / load) | 690 ms / 969 ms / **1017 ms** |
| tamanho do HTML / `<option>` totais | 260 KB / 551 |
| `#selLocalidadeJudicial` | **368 opções já no HTML** (server-rendered) |
| handlers do select | **só `change`** — nenhum `focus`/`mousedown`/`click` |
| achar "Tatuapé" com o loop `by_text` do motor | **0,5 ms** |
| atribuir valor + `change` | **3 ms** |

Ou seja: **não havia carregamento nem busca lentos**. O tempo ia para outro lugar — e a causa estava no motor:

```csharp
// ANTES (BrowserContext.Click)
page.ClickAsync(selector)                      // navega AQUI
if (waitForNavigation)
    WaitForNavigationAsync(page, timeout)       // só agora começa a ESCUTAR
```

Se o clique navega rápido (eproc: ~300 ms), o evento de navegação chega antes de alguém escutar. A espera ficava plantada até o **timeout inteiro (10 s)** e então "aceitava" via fallback de página estável — por isso ninguém notava: o fluxo *parecia* funcionar, 10 s mais lento a cada clique navegacional. A tela 1 tem dois (`click_menu` para entrar, `click_proxima` para sair) → **20 s**.

**Correção.** A espera passa a ser **armada antes** do clique, e a task é observada para não virar exceção órfã se o clique falhar. Verificação isolada (clique em `example.com` → `iana.org`): **1,0 s**.

**Lição.** Medir o que *parece* lento separa o culpado real do suspeito óbvio. As 368 opções eram inocentes.

### 4.4 "A classe não carrega" (e a área, às vezes, era engolida)

**Sintoma.** `wait_for #selIdClasseJudicial option:nth-child(2)` estourava; em outra execução, a Área aparecia como "-- Selecione uma área --" depois de setada.

**Hipóteses.** (a) AJAX lento; (b) AJAX não dispara; (c) o rebuild posterior apaga a seleção.

**Experimentos.**

1. Script que **mudava** a localidade e media o Rito: rebuild em **578 ms**, e o select **nunca ficou vazio** do ponto de vista do JS (`minOpts=3`, `:empty` nunca casou) — porque a primeira amostra só rodou **578 ms depois**, ou seja, **o AJAX do eproc bloqueia a thread principal** (XHR síncrono). Timers não correm durante o rebuild.
2. Clicando com gesto real (probe `-Click`), o Rito ficava com **0 opções no instante seguinte ao `change`** — logo, o `change` **esvazia sincronamente** e o AJAX repovoa depois.
3. Medição do encadeamento com o mecanismo do motor (`value` + `change`): Rito 3 → 0 → 3 entre **0,6 s e 3,8 s**; Área 6 → 2 em **~1,0 s**; Classe 1 → 69 em **1,0–2,6 s** (uma vez >25 s, outra >60 s).
4. `select:empty` **não serve** como sinal: marca o início do rebuild, não o fim.
5. Para a Área o esvaziamento **não é sincrono**: esperar `option[value="0"]` passava com a lista velha e o rebuild engolia a seleção (foi o caso da captura com "Área: -- Selecione uma área --").

**Conclusão.** Existem dois regimes: (i) select esvaziado sincronamente (Rito) → esperar a **opção voltar** é o sinal exato; (ii) select reconstruído depois (Área) → é preciso esperar **o rebuild acontecer** (a lista encolher) antes de setar.

**Correção.** `wait_rito_opcoes` (`option[value="2"]`), `wait_area_rebuild` (`wait_content not_contains_element` em `option:nth-child(3)`), `wait_area_opcoes`, `wait_classe_ajax`. Nenhum `delay`.

### 4.5 "Escolha um assunto." (o alerta que travava a tela 2)

**Sintoma.** `alert` do eproc após "Incluir Assunto"; o `wait_for` seguinte estourava porque **o renderer fica bloqueado** enquanto o alerta está aberto.

**Hipóteses.** (a) clique não selecionou o nó; (b) seleção perdida no re-render da árvore; (c) o botão do painel não é o da linha.

**Experimentos.** Captura do HTML da linha (`outerHTML` via `browser_execute_script` + `file_write`):

```html
<img width="11" style="display:none" id="imgIncluir02200307"
     src="infra_css/imagens/transportar.gif"
     class="inclusaoArvore" onclick="incluirAssunto('02200307')">
```

O botão da linha **nasce `display:none`** e só aparece no hover (`exibeOcultaInfos`). O "Incluir" do painel (`#btnIncluirAssunto` → `transportar()`) é para **outro** assunto; sem seleção na árvore ele só emite o alerta.

**Correção.** `dialog_action: accept` nos cliques (o alerta deixa de travar a execução) e o caminho do botão da linha (ver §4.6).

### 4.6 "getStrDesAssunto() on null" — o erro PHP que derrubava a confirmação

**Sintoma.** A página de confirmação do ajuizamento caía num erro do eproc:

```
Error: Call to a member function getStrDesAssunto() on null
/app/web/view/distribuicao/processo_confirmacao_finalizar_subfrm.php:491
```

com o resumo mostrando **"Assunto Principal:" vazio**. E, na execução anterior, o sintoma foi outro (o `#sbmConfirmar` não existia) — dois sintomas, uma causa.

**Hipóteses.** (a) o assunto não foi incluído; (b) foi incluído só no cliente; (c) o "Próxima" correu na frente do AJAX.

**Experimentos.**

1. Treino medindo os campos ocultos que carregam o assunto para o servidor:

```
ANTES do clique na linha:      - | - | - | - | -
DEPOIS (clique real na img):   82401851 | 02200307 | 02200307 | - | INDENIZAÇÃO POR DANO MORAL, …
```

2. Chamando `incluirAssunto('02200307')` **por script**: a linha e a Competência aparecem (em ~200 ms a ~1,7 s) e os campos `hdn*Assunto*` **continuam vazios**. Ou seja: **função JS ≠ gesto real**; o gesto dispara também o caminho que registra o assunto.
3. Dois segundos depois, os campos `hdn*Assunto*` já estavam **vazios de novo** (a árvore é re-renderizada) — mas `#tblAssuntoPrincipal` e `#selNumCodCompetencia` ficaram.
4. **`FormData` da página** (o experimento decisivo) mostrou quem realmente viaja:

```
hdnIdAssuntoPrincipal = 82401851
hdnListaAssuntos      = 82401851±Sim±02200307 - INDENIZAÇÃO POR DANO MORAL, …
selNumCodCompetencia  = 114
statusFiltroAssuntos  = "Assunto adicionado como principal: INDEN…"
```

**Conclusão.** O `hdnIdAssuntoPrincipal` é escrito **pelo AJAX depois do clique**. Clicar "Próxima" ~1 s após a inclusão mandava a petição **sem assunto**; o eproc só reclamava na confirmação, muito depois. O usuário tinha razão: a sequência correta é digitar → Filtrar → **esperar a árvore** → clicar "Incluir Assunto" (o da linha) → Próxima.

**Correção.**

```
fill_filtro → click_filtrar → wait_assunto (âncora visível) → hover_assunto →
click_assunto (#imgIncluir02200307, clique REAL) →
wait_assunto_efetivado (hdnIdAssuntoPrincipal não vazio, retry 20×500 ms) →
wait_incluir (#selNumCodCompetencia) → click_etapa2
```

A guarda usa o truque "falha alto sem chaves": `document.getElementById('hdnIdAssuntoPrincipal').value || (window.__assunto__nao__efetivado).x` — se estiver vazio, lança `TypeError` e o `error_handling` re-tenta.

### 4.7 "Cannot read properties of null (reading 'click')"

**Sintoma.** No fim da cadeia, o `#sbmConfirmar` não aparecia.

**Experimento.** Varredura de iframes: a confirmação **não é um diálogo**, é um **iframe**:

```html
<iframe id="ifrSubFrm" src="controlador.php?acao=processo_confirmacao_finalizar_sub…">
   └── <button id="sbmConfirmar" onclick="confirmaAjuizamento();">Confirmar ajuizamento</button>
```

Como o iframe é same-origin, dá para clicar por script. Quando a confirmação está em **página de erro** (§4.6), esse botão não existe — daí o `null.click()`.

**Correção.** `click_confirmar_ajuizamento` (`browser_execute_script` no `contentDocument` do `#ifrSubFrm`, `retry 10×1500 ms` para esperar o iframe carregar) + `wait_distribuido` (`contains_text "Processo distribuído"`) + `capturar_protocolo` (`#lblDesNumProcesso` → `PetitionNumber`).

**Lacuna de motor registrada:** nenhuma ação `browser_*` atravessa frame. Ver §5.

### 4.8 "Execution context was destroyed, most likely because of a navigation"

**Sintoma.** O último passo morria exatamente quando a página navegava para `processo_finalizado`.

**Conclusão.** `wait_content` avalia no documento; durante a navegação o contexto é destruído e a exceção subia. Mas, semanticamente, "a condição ainda não está satisfeita" — o poll deveria continuar.

**Correção.** `EvaluateWaitContent` captura erros transitórios (`IsTransientBrowserError`: *Execution context was destroyed*, *Cannot find context*, *Target closed*, *Protocol error*) e devolve `false`; o loop tenta de novo.

### 4.9 "A sonda demora 20 s antes da primeira ação"

**Sintoma.** Toda sonda gastava ~20 s antes de qualquer coisa.

**Experimento.** Comparação `dotnet run` × `dll` direto: o launcher não era (0,8 s). O tempo estava em `browser_attach_chrome`.

**Conclusão.** Em `AttachToRunning`, `timeout_ms` **não é timeout de conexão — é `Thread.Sleep` depois de anexar**. Eu passava 20000 "achando" que era timeout.

**Correção.** Remover o campo das sondas + registrar a armadilha em [guia-construcao.md](../guia-construcao.md).

### 4.10 "O documento se perde" (CKEditor)

**Sintoma.** `setData` chamado e `getData().length` devolvendo **0**; depois, o "Anexar" não abria o diálogo.

**Hipóteses.** (a) instância errada; (b) editor ainda não pronto.

**Experimentos.** `Object.keys(CKEDITOR.instances)` → `["txt_fck"]`; `status` do editor. Quando `setData` rodava antes de `status === 'ready'`, o editor se reinicializava **vazio**.

**Correção.** Duas guardas: `wait_editor_pronto` (script com retry que só passa com `status === 'ready'`) e `digitar_texto` **auto-verificável** (`setData(...) || getData().length || throw`), com retry.

### 4.11 "O clique em Anexar não abre o diálogo"

**Sintoma.** `wait_for #JQueryUIselTipoArquivo` passava, mas o `.ui-dialog-buttonpane` não existia.

**Conclusão.** O select de tipo faz parte do markup base (fica oculto) — então ele **não é sinal de diálogo aberto**; o `buttonpane` só existe quando o diálogo abre. E o clique por coordenada no botão da toolbar do CKEditor (`#cke_16`) é frágil (barra fixa / elemento no topo).

**Correção.** Clique por **elemento** (`a.cke_button__anexardocumentoesair` via `execute_script`, com retry) e guarda no `buttonpane`. Mesma lição do "Finalizar": botão que pode estar sob barra fixa → clique por elemento, não por coordenada.

### 4.12 "Documento 1 não informado."

**Sintoma.** Alerta ao clicar "Confirmar seleção de documentos", mesmo com 1 documento na tabela.

**Conclusão.** O botão (`btAcaoMovimentar()`) valida o **bloco de upload** ("Documento 1" → `input[name='file']`), não os documentos já anexados. Pelo caminho do editor o documento já entra na tabela (`hdnNumDocumentos = 1`) e esse passo **não existe**. Foi removido do fluxo.

## 5. Defeitos de motor: sintoma → causa → correção

| # | Sintoma | Causa | Correção | Onde |
|---|---|---|---|---|
| 1 | ~20 s por tela com clique navegacional | espera de navegação armada **depois** do clique | task armada antes + observação da task | `BrowserContext.Click` |
| 2 | `Execution context was destroyed` no último passo | `wait_content` não tolerava navegação | erros transitórios → `false` (poll continua) | `BrowserContext.WebAutomation.WaitContent` |
| 3 | sonda com 20 s de atraso | `browser_attach_chrome.timeout_ms` = `Thread.Sleep` pós-anexo | documentado; não usar como timeout | `AttachToRunning` |
| 4 | `confirm()`/`alert()` travavam a página | não havia tratamento de diálogo | **`dialog_action: accept`/`dismiss`** no clique | `WebAutomationAction.DialogAction`, `BrowserContext.Click` |
| 5 | popup do editor inacessível | motor só operava na página atual | **`browser_switch_page`** (`url` como filtro) | `BrowserContext.SwitchPage` |
| 6 | `input[type=file]` impossível de preencher | JS proíbe; não havia ação | **`browser_upload`** (`DOM.setFileInputFiles`) | `BrowserContext.UploadFile` |
| 7 | confirmação dentro de `<iframe>` | **não há suporte a frame** (lacuna) | contorno por `execute_script` (same-origin) | — |
| 8 | NRE ao chamar `objCarregadorSelectClasseJudicial.carregar()` | a função do eproc provoca algo que invalida o contexto | nó removido; usar espera por seletor | — |

Os itens 4, 5 e 6 entraram com paleta no Studio (`browser_upload`, `browser_switch_page`) e defaults; cobertura em `WebAutomationActionCoverageTests` (31 tipos) e `PaletteCatalogStructureTests` (31 itens).

## 6. Catálogo de armadilhas (checklist reutilizável)

### Seletores CSS
- Valor de atributo com `=` **precisa de aspas**: `a[href*="acao=processo_cadastrar"]`; sem aspas é seletor **inválido** e o `wait_for` só estoura timeout (não avisa).
- `id` começando com dígito → `[id='02200307_anchor']` (não `#02200307_anchor`).
- `:empty` marca o **início** do rebuild, não o fim — e não casa se sobra nó de texto.
- `tr:nth-child(N)` **não conta o `tbody`**: com `<thead>`/`<tfoot>`, a 1ª linha de dados é `tbody tr`. Onde há `<tfoot>` (tabela de documentos), `tbody tr` é o contador de dados.

### Selects
- `by_index` **explode** (`index out of range`) e o índice muda quando o eproc reordena; `by_value` **falha em silêncio**; `by_text` falha alto e é legível. Preferir `by_text`.
- O `change` pode **esvaziar o select dependente sincronamente** (Rito) — aí `wait_for option[...]` é sinal exato. Se o esvaziamento **não** for sincrono (Área), esperar a lista **encolher** antes de setar, senão o rebuild engole a seleção.
- Durante AJAX síncrono do eproc a **thread principal congela**: timers e polls perdem o intervalo. Por isso a espera tem que ser por seletor, não por relógio.

### Estado e verificação
- **Função JS ≠ gesto real**: `incluirAssunto()` atualiza só o DOM; o gesto dispara o caminho que grava os campos ocultos.
- A verdade está nos **campos ocultos** e no **`FormData`** — o DOM visível pode ser pré-visualização otimista.
- AJAX que grava o estado **depois** do clique → esperar o campo, nunca o "parece pronto".
- Catch-all de detecção de tela (probe de `h1`) sempre devolve algo plausível: nunca falha, nunca serve de prova.

### Gesto
- Botão que pode estar sob barra fixa → **clique por elemento** (`execute_script`), não por coordenada.
- Elemento `display:none` até hover → `browser_hover` antes, ou chamar o handler por script (**mas** ver "função ≠ gesto").
- `id` duplicado (`#btnSalvar` no topo e no rodapé) → `querySelector` pega o primeiro; é o mesmo submit.

### Janelas, frames e diálogos
- Popup vira **outra página CDP**: `browser_switch_page` para entrar **e** para voltar.
- Checkbox com handler em `click` → `browser_set_checkbox` (usa `el.click()`).
- `confirm()`/`alert()` bloqueiam o renderer: sem `dialog_action`, tudo o que vem depois estoura timeout.
- Página de erro do eproc dentro de um iframe de confirmação → sintoma "botão não existe".

## 7. Medições

| Medida | Valor |
|---|---|
| Carga da tela 1 (TTFB / DOM / load) | 690 / 969 / 1017 ms |
| HTML da tela 1 / `<option>` totais | 260 KB / 551 (368 de localidade) |
| Achar localidade (`by_text`) / aplicar | 0,5 ms / 3 ms |
| Rebuild do Rito após localidade | 0,6 s – **3,8 s** |
| Rebuild da Área após rito | ~1,0 s |
| Carga da Classe | 1,0 s – 2,6 s (já visto >25 s e >60 s) |
| Filtro de assunto → âncora visível | 0,5 s – 1,6 s |
| Inclusão do assunto → linha/competência | 0,2 s – 1,7 s (campo `hdn` logo depois) |
| `setData` no CKEditor | instantâneo (238 chars) |
| Login SSO → painel | ~19 s |
| **Cadeia completa (login → protocolo)** | **32,7 s** (era ~2 min antes do fix de navegação) |

## 8. Evidências

Petições realmente distribuídas em HML durante a construção:

| Protocolo | Como |
|---|---|
| `4000922-41.2026.8.26.0008` | caminhada manual assistida por probes |
| `4000924-11.2026.8.26.0008` | primeira cadeia completa automatizada |
| `4000925-93.2026.8.26.0008` | cadeia com os subfluxos (versão final) |

Artefatos: `output/cdp/*.png` (capturas de tela durante a investigação), `examples/eproc-peticionamento/output/eproc-*.png` (prints do fluxo em execução), `output/cdp/*.json` (dumps de DOM/estado usados nas medições), `output/probe-*` (as sondas).

## 9. Runbook: como repetir a investigação

```powershell
# 1. navegador visível com CDP (perfil próprio, o humano loga)
& "C:\Program Files\Google\Chrome\Application\chrome.exe" `
  --remote-debugging-port=9222 --remote-allow-origins=* `
  --user-data-dir="$env:TEMP\elfio-cdp-profile" https://eproc-1g-sp-hml.tjsp.jus.br/

# 2. ler a aba (nunca navega, nunca clica)
pwsh scripts/cdp-probe.ps1 -Js "document.title"
pwsh scripts/cdp-probe.ps1 -JsFile scripts/cdp-dump-controls.js      # controles + seletores
pwsh scripts/cdp-probe.ps1 -Shot output/cdp/tela.png                 # screenshot
pwsh scripts/cdp-probe.ps1 -Match "acao=processo_cadastrar_2" -JsFile x.js   # escolhe a aba

# 3. gesto real (mouse de verdade, necessário para popup/hover)
pwsh scripts/cdp-probe.ps1 -Click "#btnFiltrar"

# 4. aba congelada (background) → destravar
pwsh scripts/cdp-probe.ps1 -Match painel -Method Page.bringToFront

# 5. rodar uma etapa contra a sessão viva (~5-10 s, não distribui petição)
dotnet run --project src/Hoblio.Cli -- run output/probe-etapa1
dotnet run --project src/Hoblio.Cli -- run output/probe-etapa34

# 6. cadeia completa com tempo por ação
$sw=[Diagnostics.Stopwatch]::StartNew()
dotnet run --project src/Hoblio.Cli -- run examples/eproc-superflow 2>&1 |
  ForEach-Object { "{0,6:N2}s  {1}" -f $sw.Elapsed.TotalSeconds, $_ }
```

Detalhes que economizam tempo: passar JS por **arquivo** (`-JsFile`) evita o inferno de aspas do PowerShell; `-Method Page.handleJavaScriptDialog` com `{"accept":true}` fecha um alerta órfão; o diálogo "Ambiente eproc" do início é interativo (alguém precisa clicar OK).

## 10. Pendências e próximos passos

| Item | Detalhe |
|---|---|
| **Suporte a iframe** no motor | maior lacuna exposta: a confirmação vive em `#ifrSubFrm` e só é alcançável por `execute_script`. Sugestão: parâmetro `frame:` nas ações, ou ação `browser_switch_frame` |
| Parametrizar o assunto | código `02200307` está literal no filtro **e** no nome do botão (`#imgIncluir02200307`) → hoje trocar o assunto exige editar dois pontos |
| Perfil por descrição no login | `#tr1` escolhe **a primeira linha** da tela de perfil; para o fluxo de intimação (Chefe de Cartório) será preciso escolher por descrição + `wait_for` no rótulo |
| Latência do eproc | Classe e busca por nome variam de ~1 s a >60 s; o fluxo tolera (60 s) mas uma execução pode falhar por causa do servidor |
| Studio | as ações novas precisam de **reiniciar o Studio** para aparecer na paleta |
| Documentos duplicados | numa sessão de treino o mesmo documento foi anexado 3× (o eproc aceita). Em produção, cada documento deve ser anexado uma vez — vale uma checagem (`wait_for` do nome do arquivo antes de anexar outro) |

## 11. Cronologia dos fracassos (o que cada um ensinou)

| # | Onde falhou | Causa raiz | Lição |
|---|---|---|---|
| 1 | superfluxo: "tela inesperada Seleção de perfil" | `ScreenText` era cache de probe | handoff = pós-condição verificada |
| 2 | etapa 1: `index out of range` | `by_index` em lista vazia; classe no índice errado | `by_text` + esperar o AJAX |
| 3 | etapa 2: `Escolha um assunto` | clique no painel sem seleção; botão da linha oculto até hover | capturar o `outerHTML` da linha |
| 4 | etapa 3: grade de resultado não apareceu | busca lenta + alerta bloqueando | `dialog_action` + tolerância maior |
| 5 | etapa 5: documento não anexado | `setData` antes de `ready` | guarda de prontidão + `setData` auto-verificável |
| 6 | etapa 5: `#sbmConfirmar` ausente | confirmação em iframe de página de erro | ler o `FormData` para achar o estado real |
| 7 | confirmação: `getStrDesAssunto() on null` | assunto não efetivado no servidor | gesto real + esperar `hdnIdAssuntoPrincipal` |
| 8 | último passo: `Execution context was destroyed` | `wait_content` durante navegação | tolerar erro transitório |
| 9 | cadeia: ~2 min | espera de navegação armada tarde | medir por ação, não o total |
| 10 | após a divisão em subfluxos: âncora não apareceu | 11 nós com timeout de 30 s esquecido | padronizar o piso de timeout |
