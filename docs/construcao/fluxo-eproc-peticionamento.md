# Fluxo eproc: peticionamento (etapas 1–2 de 4)

## Objetivo

Primeiro **fluxo principal chamado** de verdade do repositório: ele **não navega nem faz login**. Assume a tela deixada por [`examples/login-eproc`](../../examples/login-eproc) — painel do advogado já logado — e executa a abertura do peticionamento até a inclusão do assunto.

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
| herda | `ScreenText` | texto da tela detectada no login (`detect_screen`); o superfluxo decide o ramo por ele |
| herda | `EprocUrl`, `EprocUser` | ambiente/usuário escolhidos no login |
| entrega | `PetitionNumber` | **ainda não preenchido** — o número do protocolo só existe na fase 4. Enquanto isso o log do superfluxo mostra `protocolo` vazio; é esperado, não é bug |

Escopo: `inherit` (padrão). É o que faz o navegador ser o mesmo objeto e a tela ser "herdada" — ver [composicao-superfluxos.md](./composicao-superfluxos.md) §6.

## Etapas implementadas

| # | Nó (`flow.json`) | Ação | Seletor / valor | Observação |
|---|------------------|------|-----------------|------------|
| — | `wait_menu` | `browser_wait_for` | `#main-menu > li:nth-child(16) > a` | guarda do handoff: só existe no painel logado |
| — | `log_handoff` | `log` | — | registra tela/usuário herdados |
| 1 | `click_menu` | `browser_click` | mesmo seletor, `wait_selector_after: #selLocalidadeJudicial` | abre o peticionamento |
| 2 | `sel_localidade` | `browser_set_dropdown` | `by_value` = `8` | São Paulo – Regional VIII – Tatuapé (`option:nth-child(320)`) |
| 3 | `sel_rito` | `browser_set_dropdown` | `by_index` = `2` | `option:nth-child(3)` |
| 4 | `sel_area` | `browser_set_dropdown` | `by_index` = `2` | Cível, `option:nth-child(3)` |
| 5 | `sel_classe` | `browser_set_dropdown` | `by_index` = `55` | Petição Cível, `option:nth-child(56)` |
| — | `wait_load_classe` | `delay` 2500 ms | — | a classe dispara carregamento de 1–2 s |
| 6 | `check_nao_aplica` | `browser_set_checkbox` | `#chkNaoAplicaValor`, `checked: true` | só clica se o estado diferir |
| 7 | `click_proxima` | `browser_click` | `#btnSalvar`, `wait_selector_after: #txtFiltroPesquisa` | botão "Próxima" |
| 8 | `fill_filtro` | `browser_fill` | `#txtFiltroPesquisa` = `02200307` | — |
| 8b | `press_enter` | `browser_execute_script` | dispara `keydown`/`keypress`/`keyup` com `keyCode 13` | não existe ação de teclado no motor — ver abaixo |
| 8c | `wait_assunto` | `browser_wait_for` | `[id='02200307_anchor'] > span > span`, `wait_visible: true` | a árvore só aparece depois do filtro |
| 8d | `click_assunto` | `browser_click` | mesmo seletor | seleciona o assunto |
| 9 | `click_incluir` | `browser_click` | `#btnIncluirAssunto` | + 1,5 s de load (`wait_incluir`) |
| 10 | `click_etapa2` | `browser_click` | `#sbmProcessoEtapa2`, `wait_for_navigation: true` | vai para a fase 3 |
| — | `shot_etapa2` | `browser_screenshot` | `output/eproc-fase3.png` | debug: retrato da fase 3 |
| — | `log_parcial` | `log` | — | avisa que a petição **não** foi protocolada |

## Quatro decisões que não são óbvias

### 1. `by_index` é 0-based

`browser_set_dropdown` faz `el.selectedIndex = i`, portanto o índice é **zero-based**, enquanto no CSS `option:nth-child(N)` é 1-based:

```
option:nth-child(3)   → dropdown_value: "2"
option:nth-child(56)  → dropdown_value: "55"
```

Onde o `value` da opção é conhecido, ele é preferível (a localidade usa `by_value: "8"` em vez de índice 319 — imune a mudança na ordem da lista). Os índices de Rito/Área/Classe vieram da tela e são o elo mais frágil do fluxo: se o eproc reordenar as opções, o fluxo passa a selecionar outra coisa silenciosamente. Trocar por `by_value`/`by_text` é a evolução recomendada (ver *Verificação pendente*).

### 2. Enter: não há ação de teclado

O módulo WebAutomation não tem `keypress` (`browser_press_button` é **clique**, não tecla). A solução foi `browser_execute_script` disparando os três eventos com `keyCode`/`which` 13 — que é o que jQuery/handlers legados leem:

```json
{
  "type": "browser_execute_script",
  "script": "(el => (el.focus(), ['keydown','keypress','keyup'].forEach(t => el.dispatchEvent(new KeyboardEvent(t, Object.fromEntries([['key','Enter'],['code','Enter'],['keyCode',13],['which',13],['bubbles',true],['cancelable',true]])))), 'enter enviado'))(document.querySelector('#txtFiltroPesquisa'))",
  "variable": "FiltroAssuntoResultado"
}
```

Se o eproc só reagir a tecla nativa, `wait_assunto` estoura em 30 s — plano B: `browser_fill` com `text: "02200307\r"` (o Puppeteer traduz `\r` para Enter).

### 3. O script **não pode conter `{` nem `}`**

`RunVariables.Interpolate` varre chaves de forma ingênua: qualquer `{...}` é tratado como referência de variável e **lança** `invalid variable name` quando o conteúdo não é um identificador. Por isso o script acima não usa object literal (`{ key: 'Enter' }`) nem arrow com corpo em bloco — usa `Object.fromEntries([[...]])` e arrow de expressão. Vale para **todo** `browser_execute_script` do projeto.

Regra prática: dentro de `script`, zero chaves; arrays e `Object.fromEntries` no lugar de objetos.

### 4. Seletor do assunto: atributo em vez de escape CSS

A tela fornece `#\30 2200307_anchor` — escape CSS para um `id` que começa com dígito (`\30 ` = `0`). É frágil (espaço terminador, e a barra invertida teria de ser duplicada no JSON). Foi trocado pelo equivalente estável:

```
[id='02200307_anchor'] > span > span
```

## Retentativas (listas que carregam por AJAX)

As listas de Rito/Área/Classe são recarregadas a cada seleção; quando o `change` retorna antes do AJAX popular o próximo `select`, o `by_index` falha com `index out of range`. Por isso:

| Nó | `retry_count` | `retry_delay_ms` | Motivo |
|----|---------------|------------------|--------|
| `sel_rito` | 2 | 1200 | lista depende da localidade |
| `sel_area` | 2 | 1200 | lista depende do rito |
| `sel_classe` | 4 | 1500 | lista depende da área (a mais lenta) |
| `check_nao_aplica` | 2 | 1000 | checkbox pode ser recriado no load da classe |

`browser_set_dropdown` dispara `Event('change', {bubbles:true})` — sem isso os selects encadeados não reagiriam.

## Como executar

```powershell
# 1. validar contrato e grafo (sem rede)
dotnet run --project src/Hoblio.Cli -- validate examples/eproc-superflow

# 2. executar a cadeia (precisa de Chrome + credenciais do eproc)
dotnet run --project src/Hoblio.Cli -- run examples/eproc-superflow
```

Rodar `examples/eproc-peticionamento` **sozinho** falha de propósito em `wait_menu`: sem `browser_navigate`/attach não há sessão (`no browser session: run browser_navigate before other browser actions`). Este fluxo só existe dentro da cadeia.

## Verificação pendente (importante)

Os seletores, os três índices e o comportamento do Enter **não foram conferidos contra a página real** — não houve acesso ao DOM durante a construção. Duas formas de fechar isso:

1. **CDP**: iniciar o Chrome com `--remote-debugging-port=9222` e usar `browser_attach_chrome` (ou um probe CDP) para enumerar `value`/texto das opções e trocar os `by_index` por `by_value`; `Page.captureScreenshot` permite anexar imagem ao diagnóstico.
2. **HTML colado**: `outerHTML` dos quatro `<select>` e do nó do assunto — basta para derivar os valores exatos.

Se o run parar, `output/eproc-fase3.png` mostra a tela em que parou.

## Pendente: fases 3 e 4

Faltam três fases após `#sbmProcessoEtapa2`. Para cada uma é preciso: seletor, ação, e o **sinal de que carregou** (elemento que só existe depois). Enquanto isso, o fluxo encerra registrando que a petição não foi protocolada, e `PetitionNumber` permanece vazio.
