# Construção — runtime, catálogo e observabilidade

Documentação de **decisões de arquitetura** e **contratos** que orientam evolução do Elfio (modelo, runtime, CLI, Studio). Complementa [ARCHITECTURE.md](../ARCHITECTURE.md) (mapa de projetos) e [guia-construcao.md](../guia-construcao.md) (build, testes, histórico de entregas).

| Documento | Conteúdo |
|-----------|----------|
| [runtime-e-execucao.md](./runtime-e-execucao.md) | Camadas, pipeline de run, separação grafo × ações × serviços |
| [catalogo-acoes-e-modulos.md](./catalogo-acoes-e-modulos.md) | Identificadores estáveis (`module`, `action_id`), paleta e mapeamento JSON |
| [modulo-excel.md](./modulo-excel.md) | Módulo Excel COM: paleta, JSON, runtime, exemplo, testes |
| [modulo-word.md](./modulo-word.md) | Módulo Word COM: 8 ações PAD, JSON, runtime |
| [modulo-web-automation.md](./modulo-web-automation.md) | Automação do navegador (CDP): 27 ações PAD, PuppeteerSharp |
| [notas-release-2026-11-05.md](./notas-release-2026-11-05.md) | Rodada nov/2025: lista, login-eproc, Excel, Studio |
| [telemetria-e-logs.md](./telemetria-e-logs.md) | Eventos de run, JSONL, correlação Studio ↔ CLI |
| [manifest-e-capacidades.md](./manifest-e-capacidades.md) | `manifest.json` atual e extensão `module_references` |

**Estado:** telemetria enriquecida (`module`, `action_id`, índices) e `module_references` no manifest estão **implementadas** (`ActionCatalog`, `RunEventJson`, `ModuleManifestSync` no save). Projetos antigos sem esses campos continuam válidos até o próximo save.

**Nov/2026:** Switch/Case no runtime e Studio; correção de stack overflow no save com subfluxos embutidos; registro completo em [guia-construcao.md](../guia-construcao.md) § *Rodada nov/2026 — Switch/Case, save e shell*.
