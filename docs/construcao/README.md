# Construção — runtime, catálogo e observabilidade

Documentação de **decisões de arquitetura** e **contratos** que orientam evolução do Elfio (modelo, runtime, CLI, Studio). Complementa [ARCHITECTURE.md](../ARCHITECTURE.md) (mapa de projetos) e [guia-construcao.md](../guia-construcao.md) (build, testes, histórico de entregas).

| Documento | Conteúdo |
|-----------|----------|
| [runtime-e-execucao.md](./runtime-e-execucao.md) | Camadas, pipeline de run, separação grafo × ações × serviços |
| [catalogo-acoes-e-modulos.md](./catalogo-acoes-e-modulos.md) | Identificadores estáveis (`module`, `action_id`), paleta e mapeamento JSON |
| [telemetria-e-logs.md](./telemetria-e-logs.md) | Eventos de run, JSONL, correlação Studio ↔ CLI |
| [manifest-e-capacidades.md](./manifest-e-capacidades.md) | `manifest.json` atual e extensão `module_references` |

**Estado:** telemetria enriquecida (`module`, `action_id`, índices) e `module_references` no manifest estão **implementadas** (`ActionCatalog`, `RunEventJson`, `ModuleManifestSync` no save). Projetos antigos sem esses campos continuam válidos até o próximo save.
