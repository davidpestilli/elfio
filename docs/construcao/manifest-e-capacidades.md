# Manifest do projeto e referências de módulo

## Objetivo

O **`manifest.json`** descreve o projeto e, além do grafo principal, lista **fluxos adicionais** (`flows`). Para runs reproduzíveis e validação antecipada, o manifest deve também registrar **quais capacidades (módulos lógicos) o projeto exige** — independentemente de onde as ações estão implementadas hoje (monolito `Hoblio.Runtime`).

---

## Formato atual (estável)

```json
{
  "name": "demo-flow",
  "version": "0.1.0",
  "description": "…",
  "entry_flow": "main",
  "flows": {
    "greet": {
      "path": "flows/greet.json",
      "inputs": ["name"],
      "outputs": ["message"]
    }
  }
}
```

| Campo | Função |
|-------|--------|
| `name` | Identificador do projeto; usado em `RunStarted` |
| `version` | Versão semântica do pacote de fluxo |
| `description` | Texto livre |
| `entry_flow` | Id do fluxo principal quando há `flows` |
| `flows` | Mapa id → `{ path, inputs?, outputs?, description? }` |
| `linked_projects` | Mapa id → `{ path, flow?, description?, requires?, exports? }` — projetos externos chamáveis por `run_flow.project` (ver [composicao-superfluxos.md](./composicao-superfluxos.md)) |

Variáveis iniciais de run ficam em **`flow.json`** → `variables`, não no manifest.

Implementação: [`FlowManifest`](../../src/Hoblio.Model/Project.cs), carga em [`ProjectLoader`](../../src/Hoblio.Serde/ProjectLoader.cs).

---

## `module_references` (implementado no save)

Motivação:

- Detectar **dependências ausentes** antes do run (ex.: projeto usa browser mas Chrome não configurado).
- Fixar **versão do engine** que gravou o projeto.
- Preparar **plugins** por assembly sem mudar ids no grafo.

Campos (opcionais na leitura; preenchidos ao salvar via `ModuleManifestSync.Refresh`):

```json
{
  "name": "login-eproc",
  "version": "0.2.0",
  "engine_version": "0.1.0",
  "created_engine_version": "0.1.0",
  "module_references": [
    {
      "name": "WebAutomation",
      "assembly_name": "Hoblio.Runtime",
      "version": "0.1.0",
      "type": 1
    },
    {
      "name": "Variables",
      "assembly_name": "Hoblio.Runtime",
      "version": "0.1.0",
      "type": 1
    }
  ]
}
```

| Campo | Significado |
|-------|-------------|
| `engine_version` | Versão do runtime Elfio compatível com o pacote |
| `created_engine_version` | Versão ativa quando o projeto foi criado ou regravado |
| `module_references[].name` | Módulo lógico — alinha com `module` no [catálogo](./catalogo-acoes-e-modulos.md) |
| `module_references[].assembly_name` | Origem da implementação (hoje sempre `Hoblio.Runtime`; futuro: `Hoblio.Modules.Browser`) |
| `module_references[].version` | Versão do módulo ou binding |
| `module_references[].type` | `1` = módulo built-in; reservar outros valores para extensões custom |

---

## Derivação automática

**Quando preencher:**

- `ProjectLoader.SaveProject` (Studio: `SaveAllFlowFiles` antes do save para incluir flows auxiliares no disco).
- CLI: `hoblio validate <dir> --update-manifest` (só manifest; não altera `flow.json`).

Algoritmo (`ModuleManifestSync` + `ActionCatalog.CollectModulesFromGraph`):

1. Carregar todos os grafos do projeto (`flow.json` + entradas de `manifest.flows` + subfluxos embutidos).
2. Para cada nó:
   - `action` → mapear `type` → `module` (catálogo).
   - `if` / `switch` / `case` / `case_default` / loops → módulos virtuais `Conditionals`, `Loops`.
   - `run_flow` → incluir `FlowControl`.
3. Subfluxos **embutidos**: percorrer cada entrada de `flow.subflows` com `SubflowBundle.AsGraph(null)` — **não** re-injetar o mapa completo de irmãos em cada bundle (evita recursão infinita ao agregar módulos).
4. Unificar por `name`; ordenar; gravar se diferença em relação ao manifest salvo.

Validação opcional: se o grafo usa `module` X e `module_references` não contém X → aviso (não erro fatal na fase 1).

---

## Pacote de run (futuro)

Runs publicados ou executados fora do workspace podem incluir **`runtime.assembly_refs`** — lista resolvida de assemblies no momento do run:

```json
[
  {
    "path": "C:\\…\\Hoblio.Runtime.dll",
    "name": "Hoblio.Runtime",
    "version": "0.1.0",
    "is_module": true
  }
]
```

Uso: ambientes CI, agent headless, auditoria de “o que rodou de fato”. Não substitui `module_references` no manifest; complementa o pacote efêmero de execução.

---

## Relação com `flow.json`

| Artefato | Escopo |
|----------|--------|
| `manifest.json` | Projeto, multi-flow, capacidades, versão engine |
| `flow.json` | Um grafo: `start`, `nodes`, `edges`, `variables`, `subflows` embutidos |

O manifest **não** duplica o grafo; apenas referencia arquivos filhos e capacidades agregadas.

---

## Migração

1. **Fase 0 (atual):** sem `module_references`; validação só estrutural.
2. **Fase 1:** Studio preenche `module_references` no save; CLI ignora se ausente.
3. **Fase 2:** `validate` falha ou avisa em mismatch grafo ↔ manifest.
4. **Fase 3:** módulos em assemblies separados; `assembly_name` distinto por entrada.

Nenhuma fase exige regravar exemplos em massa: primeira abertura/save no Studio atualiza o manifest.
