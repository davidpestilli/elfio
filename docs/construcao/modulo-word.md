# Módulo Word (`word_*`)

Automação do **Microsoft Word** no Windows via **COM** (`Word.Application`), alinhada à paleta PAD (8 ações). Telemetria: módulo **`Word`**, `action_id` como `LaunchWord`, `ReadFromWord`, etc.

**Implementação:** código próprio em `Hoblio.Runtime` (`WordContext`); não há cópia de assemblies ou IL de terceiros. Baseline funcional: estudo PAD **`26-Word`** (`LaunchWord`, `WordOperationsFacade`, modos Robin → campo `mode` no JSON).

---

## Requisitos

- Windows
- Microsoft Word instalado (`ProgID` `Word.Application`)
- Execução local (Studio ou CLI)

---

## Paleta (Studio)

Grupo **Word** com 8 entradas (`WordPaletteCatalog.cs`):

| Ação Studio | `type` | `action_id` |
|-------------|--------|-------------|
| Iniciar o Word | `word_launch` | `LaunchWord` |
| Anexar ao Word em execução | `word_attach` | `AttachToRunningWord` |
| Salvar o Word | `word_save` | `SaveWord` |
| Fechar o Word | `word_close` | `CloseWord` |
| Ler o documento do Word | `word_read` | `ReadFromWord` |
| Escrever em documento do Word | `word_write` | `WriteToWord` |
| Inserir imagem… | `word_insert_image` | `InsertImageToWord` |
| Localizar e substituir… | `word_find_replace` | `FindAndReplaceWord` |

---

## Modelo

- **`WordAction`** — campos compartilhados; discriminant `type`
- **`WordActionTypes`** — 8 constantes + `ModuleActionIds`
- **`WordEnums`** — modos documentados (`mode`, `image_source`)

### Campos JSON principais

| Campo | Uso |
|-------|-----|
| `instance` | Sessão Word (saída em `word_launch`, entrada nas demais) |
| `path` | Abrir, Salvar como, imagem (ficheiro) |
| `document_name` | Anexar por nome/caminho do documento |
| `mode` | Variantes read/write/close/insert (ver `WordEnums`) |
| `pages` | Leitura por página (`1-3,5`) |
| `bookmark` | Marcador Word |
| `value` | Texto a escrever |
| `output_variable` | Texto lido |
| `find_text` / `replace_text` | Localizar e substituir |
| `all_matches`, `match_wildcards`, `match_case`, `match_entire_word` | Flags de busca |
| `image_source` | `file` ou `clipboard` |
| `text_to_find` | Posição de imagem relativa a texto |

---

## Runtime

1. `word_launch` / `word_attach` gravam id de sessão em `instance`
2. Sessão mantém `Application` + `Document` (anexo por documento, como PAD)
3. Anexo: `GetActiveObject` + varredura `WINWORD` via `AccessibleObjectFromWindow` quando necessário
4. `word_close` fecha documento; encerra Word se não restarem documentos

Modos PAD espelhados via `mode` (ex.: leitura `document` | `pages` | `bookmark`; escrita `start` | `end` | `bookmark_start` | `bookmark_end`; fechar `close` | `save` | `save_as`; imagem `document_start` | `document_end` | `bookmark_*` | `text_*`).

Variantes Robin do PAD (12 modos de inserir imagem, 4 de localizar/substituir, etc.) colapsam em **8 tipos** `word_*` na paleta; o detalhe fica em `mode` / flags JSON — ver `WordEnums.cs`.

---

## Arquivos (implementação)

| Camada | Ficheiros |
|--------|-----------|
| Modelo | `WordAction.cs`, `WordActionTypes.cs`, `WordEnums.cs`, `WordActionMutator.cs`, `Json/WordActionJson.cs` |
| Runtime | `WordContext.cs` — dispatch COM, sessão `Application` + `Document`, anexo WINWORD |
| Integração | `ActionCatalog`, `ActionKindJsonConverter`, `ActionExecutor`, `FlowExecutor`, `TemplateReferenceValidator` |
| Studio | `WordPaletteCatalog.cs`, `WordActionDefaults.cs`, `FlowEditorViewModel.WordFields.cs`, `PropertiesPanel.xaml` |
| Testes | `WordActionCoverageTests`, `ActionKindTests` (Word), `PaletteCatalogStructureTests` (grupo Word) |

---

## Studio — propriedades

Painel **Word** no Properties: campos compartilhados (preencher só o que a ação usa). Defaults na paleta: `WordActionDefaults.TryCreate`.

---

## Manifest

Projetos com ações Word incluem `module_references` com `"name": "Word"` após validate/save (ex.: `examples/word-demo-flow`).

---

## Exemplo

**`examples/word-demo-flow`**

Sequência: Launch → escrever no fim do documento → localizar/substituir → ler documento inteiro → log → SaveAs `output/word-demo.docx` → Close.

```powershell
dotnet run --project src/Hoblio.Cli -- validate examples/word-demo-flow
dotnet run --project src/Hoblio.Cli -- run examples/word-demo-flow --jsonl
```

---

## Testes

| Teste | O quê |
|-------|--------|
| `WordActionCoverageTests` | 8 tipos + `ModuleActionIds` |
| `ActionKindTests.Word_*` | Round-trip JSON + catálogo |
| `PaletteCatalogStructureTests` | 1 grupo Word, 8 itens |

```powershell
dotnet test --filter "Word|PaletteCatalog"
```

---

## Adicionar ou alterar uma ação Word

1. Constante + `ModuleActionIds` em `WordActionTypes.cs`
2. Entrada em `WordPaletteCatalog.cs`
3. `WordActionJson` / `WordActionDefaults` se novos campos
4. Ramo em `WordContext.Execute`
5. Campos no painel Word, se necessário
6. Teste de round-trip JSON
7. Linha neste doc e em [catalogo-acoes-e-modulos.md](./catalogo-acoes-e-modulos.md)
