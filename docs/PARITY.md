# Checklist de paridade (Rust → C#)

Atualize este arquivo quando fechar gaps. Verificação automática: `scripts/verify-parity.ps1` + `dotnet test`.

## Modelo e persistência

| Item | Rust | C# | Verificação |
|------|------|-----|-------------|
| ActionKind (todas variantes slice 1–8) | ✓ | ✓ | testes JSON + load examples |
| ControlKind If/ForEach/While/Repeat | ✓ | ✓ | flow-control-demo |
| EdgePort + `resolved_port` | ✓ | ✓ | validate + demos |
| ErrorHandling retry/continue | ✓ | ✓ | resilience-demo |
| validate_graph + validate_project | ✓ | ✓ | unit + validate CLI |
| `{ref}` templates (refs conhecidas) | ○ | ✓ | `TemplateReferenceValidator` + testes |
| load/save project folder | ✓ | ✓ | ProjectLoader |

## Runtime

| Item | Rust | C# | Verificação |
|------|------|-----|-------------|
| Log, delay, noop | ✓ | ✓ | demo-flow |
| Set/increase/decrease variable | ✓ | ✓ | variables-loops-demo |
| File read/write/append | ✓ | ✓ | files-lists-demo |
| List append/clear | ✓ | ✓ | files-lists-demo |
| Variáveis PAD (`variables_*`, 31 + escalares/listas legadas) | — | ✓ | `VariablesBehaviorTests`, `VariablesActionCoverageTests`, `variables-demo-flow` |
| HTTP GET | ✓ | ✓ | web-demo-flow (manual); C# opcional `variable` no JSON |
| HTTP REST (`http_request`: método, Accept, cabeçalhos, corpo, status + corpo + cabeçalhos resposta) | — | ✓ | `HttpRequestRuntimeTests` + paridade PAD Web/InvokeWebService |
| LLM (`llm_completion`: Ollama `/api/chat`, OpenAI-style chat, prompt + documento + JSON opcional) | — | ✓ | `LlmCompletionRuntimeTests`; doc [modulo-llm.md](construcao/modulo-llm.md) |
| PDF (`pdf_*`: texto, tabelas heurísticas, imagens, extract páginas, merge; PdfPig) | — | ✓ | `PdfRuntimeTests`; doc [modulo-pdf.md](construcao/modulo-pdf.md) |
| If / ForEach / While / Repeat | ✓ | ✓ | DemoFlowTests |
| Next loop / Exit loop | ✓ | ✓ | flow-control (se usado no demo) |
| Browser CDP (`browser_*`, 29 ações PAD + attach) | ✓ | ✓ | browser-demo-flow (manual + Chrome); attach + `--remote-debugging-port` (a sessão CDP abre pelo Studio **Navegador → Abrir CDP…**, por `hoblio browser` ou `scripts/start-cdp-browser.ps1`) |
| show_message / show_get_user_input / show_select_from_list | ✓ | ✓ | message-timer-demo (manual) |
| Excel COM (`excel_*`, 41 ações) | — | ✓ | `excel-demo-flow` (manual + Excel instalado) |
| Word COM (`word_*`, 8 ações) | — | ✓ | `word-demo-flow` (manual + Word instalado) |
| HOBLIO_CHROME_PATH / HOBLIO_BROWSER_DEBUG_* | ✓ | ✓ | ChromiumLocator / attach CDP |
| Composição de fluxos principais (`run_flow.project` + `linked_projects`) | — | ✓ | `SuperflowTests`, `superflow-demo` |
| Cancel run | ✓ | ✓ | API `RunCancel` (Studio a expor) |

## CLI

| Item | Rust | C# |
|------|------|-----|
| `hoblio validate <dir>` | ✓ | ✓ |
| `hoblio validate --update-manifest` | — | ✓ |
| `hoblio run <dir>` | ✓ | ✓ |
| `--jsonl` | ✓ | ✓ (`RunEventJson`, formato Studio) |

## Studio (UI)

| Item | React Flow (Rust) | WPF + React Flow (C#) |
|------|-------------------|-----------------|
| Canvas flowchart | ✓ | ✓ |
| Abrir/salvar projeto | ✓ | ✓ |
| Validar / executar | ✓ | ✓ |
| Posição UI (drag) | ✓ | ✓ (React Flow) |
| Palette agrupada / novos nós | ✓ | ✓ (categorias + subgrupos ex. Excel Básico/Avançado; busca; Start/End/New subflow) |
| Modo Exibição / Edição | ✓ | ✓ |
| PropertiesPanel completo | ✓ | ✓ (formulário por tipo de ação, if/loop, run_flow, erro, última exec.) |
| PropertiesPanel Variáveis contextual | — | ✓ | `VariablesActionUi`, `ShowVarSection*`; doc [modulo-variaveis.md](construcao/modulo-variaveis.md) |
| Run log panel | ✓ | ✓ (lista + toggle JSON + copiar log) |
| Waypoint edges (arrastar / + / duplo clique / viewport) | ✓ | ✓ |
| Handles multi-lado (then/else/body/after) | ✓ | ✓ |
| Reconectar arestas (`edgesReconnectable`) | ✓ | ✓ |
| Substituir ligação na mesma porta | ✓ | ✓ |
| Snap grid 10×10 + guias de alinhamento | ✓ | ✓ |
| `fitView` ao abrir fluxo / aba | ✓ | ✓ (`fitViewToken`) |
| Delete nós (protege `start`) + arestas | ✓ | ✓ |
| Atalhos copiar/colar/recortar/selecionar tudo | ✓ | ✓ (bridge → WPF) |
| Toolbar canvas (modo, undo/redo, limpar, rótulo de aresta) | ✓ | ✓ |
| Subflows embutidos / abas | ✓ | ✓ |
| `run_flow` (runtime + Studio) | ✓ | ✓ (painel: flow_id/path, scope, share_browser, inputs/outputs, abrir subfluxo) |
| Renomear subfluxo + patch `run_flow` | ✓ | ✓ (UI inline + `SubflowDocumentOpsTests`) |
| PropertiesPanel (canvas vs nó, variáveis, subfluxos) | ✓ | ✓ (Flow variables + gestão subfluxo; ligações no canvas) |
| `manifest.flows` (abas + save multi-arquivo) | ✓ | ✓ |
| Variáveis de projeto | ✓ | ✓ (editor JSON) |
| Stop / cancelar run | ✓ | ✓ (delay cooperativo + RunCancel) |
| Desfazer / refazer / limpar fluxo | ✓ | ✓ |
| Destaque nó em execução | ✓ | ✓ |
| Aviso não salvo | ✓ | ✓ |

Legenda: ✓ implementado · ○ pendente / simplificado

### Studio — gaps residuais (vs Rust)

| Item | Notas |
|------|--------|
| Ramo On error dedicado no grafo | Fora de escopo nos dois stacks |
| MiniMap / Controls / panOnScroll | ✓ paridade visual; sensibilidade pode diferir levemente |
## Testes espelhados do Rust

| Teste Rust (`hoblio-runtime`) | Teste C# |
|-------------------------------|----------|
| `flow_control_demo_runs` | `DemoFlowTests.Flow_control_demo_runs` |
| `variables_loops_demo_runs` | `DemoFlowTests.Variables_loops_demo_runs` |
| `files_lists_demo_runs` | `DemoFlowTests.Files_lists_demo_runs` |
| `resilience_demo_runs` | `DemoFlowTests.Resilience_demo_runs` |
| `action.rs` (5 JSON roundtrips) | `ActionKindTests` (5 facts) |
| `expr` / `variables` / `lists` / `adjust_var` unit | `UnitBehaviorTests` (+3) |

Contagem atual: **124** testes automatizados (`dotnet test` na solution; nov/2026).

Verificação local: `.\scripts\verify-parity.ps1` (Release build + testes + validate nos demos incl. `variables-demo-flow` + probe browser).

Gaps detalhados do módulo Variáveis: [construcao/modulo-variaveis.md](construcao/modulo-variaveis.md) § *Pendências e gaps*.

Scripts Grok: `scripts/eval-flow-control.ps1` e `scripts/eval-rpa-abc.ps1` (C#); comparativo cross-stack: `eval-grok-cross-stack.ps1` (ver [guia-construcao.md](guia-construcao.md)).
