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
| show_message / show_get_user_input | ✓ | ✓ | message-timer-demo (manual) |
| HOBLIO_CHROME_PATH | ✓ | ✓ | ChromiumLocator |
| Cancel run | ✓ | ✓ | API `RunCancel` (Studio a expor) |

## CLI

| Item | Rust | C# |
|------|------|-----|
| `hoblio validate <dir>` | ✓ | ✓ |
| `hoblio run <dir>` | ✓ | ✓ |
| `--jsonl` | ✓ | ✓ (`RunEventJson`, formato Studio) |

## Studio (UI)

| Item | React Flow (Rust) | WPF Nodify (C#) |
|------|-------------------|-----------------|
| Canvas flowchart | ✓ | ✓ |
| Abrir/salvar projeto | ✓ | ✓ |
| Validar / executar | ✓ | ✓ |
| Posição UI (drag) | ✓ | ✓ (Nodify) |
| Palette agrupada / novos nós | ✓ | ✓ (categorias PAD + filtro; Start/End/New subflow alinhados ao Rust) |
| Modo Exibição / Edição | ✓ | ✓ |
| PropertiesPanel completo | ✓ | ✓ (formulário por tipo de ação, if/loop, run_flow, erro, última exec.) |
| Run log panel | ✓ | ✓ (lista + toggle JSON + copiar log) |
| Waypoint edges (arrastar / + / duplo clique / viewport) | ✓ | ✓ |
| Subflows embutidos / abas | ✓ | ✓ |
| `run_flow` (runtime + Studio) | ✓ | ✓ (painel: flow_id/path, scope, share_browser, inputs/outputs, abrir subfluxo) |
| Renomear subfluxo + patch `run_flow` | ✓ | ✓ (UI inline + `SubflowDocumentOpsTests`) |
| PropertiesPanel (canvas vs nó, variáveis, subfluxos) | ✓ | ✓ (nova ligação + ligação selecionada no painel) |
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
## Testes espelhados do Rust

| Teste Rust (`hoblio-runtime`) | Teste C# |
|-------------------------------|----------|
| `flow_control_demo_runs` | `DemoFlowTests.Flow_control_demo_runs` |
| `variables_loops_demo_runs` | `DemoFlowTests.Variables_loops_demo_runs` |
| `files_lists_demo_runs` | `DemoFlowTests.Files_lists_demo_runs` |
| `resilience_demo_runs` | `DemoFlowTests.Resilience_demo_runs` |
| `action.rs` (5 JSON roundtrips) | `ActionKindTests` (5 facts) |
| `expr` / `variables` / `lists` / `adjust_var` unit | `UnitBehaviorTests` (+3) |

Contagem atual: **45** testes automatizados (`dotnet test` na solution).

Verificação local: `.\scripts\verify-parity.ps1` (Release build + 45 testes + validate nos 9 demos + probe browser).

Scripts Grok: `scripts/eval-flow-control.ps1` e `scripts/eval-rpa-abc.ps1` (C#); comparativo cross-stack: `eval-grok-cross-stack.ps1` (ver [guia-construcao.md](guia-construcao.md)).
