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
| HTTP GET | ✓ | ✓ | web-demo-flow (manual); C# opcional `variable` no JSON |
| If / ForEach / While / Repeat | ✓ | ✓ | DemoFlowTests |
| Next loop / Exit loop | ✓ | ✓ | flow-control (se usado no demo) |
| Browser CDP (navigate, click, fill, wait, get_text) | ✓ | ✓ | browser-demo-flow (manual + Chrome) |
| show_message / show_get_user_input / show_select_from_list | ✓ | ✓ | message-timer-demo (manual) |
| Excel COM (`excel_*`, 41 ações) | — | ✓ | `excel-demo-flow` (manual + Excel instalado) |
| HOBLIO_CHROME_PATH | ✓ | ✓ | ChromiumLocator |
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

Contagem atual: **54** testes automatizados (`dotnet test` na solution).

Verificação local: `.\scripts\verify-parity.ps1` (Release build + 45 testes + validate nos 9 demos + probe browser).

Scripts Grok: `scripts/eval-flow-control.ps1` e `scripts/eval-rpa-abc.ps1` (C#); comparativo cross-stack: `eval-grok-cross-stack.ps1` (ver [guia-construcao.md](guia-construcao.md)).
