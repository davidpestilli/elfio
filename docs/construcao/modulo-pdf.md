# Módulo PDF (`pdf_*`)

Operações sobre ficheiros **PDF** sem Adobe — biblioteca [UglyToad.PdfPig](https://github.com/UglyToad/PdfPig) no runtime (NuGet `UglyToad.PdfPig` 1.7.0-custom-5). Telemetria: módulo **`PDF`**; `action_id` conforme tabela abaixo (paridade de nomes com o grupo **PDF** do PAD onde aplicável).

**Limitações:** não faz **OCR** (digitalizações só com imagem continuam sem texto). **`pdf_extract_tables`** é **heurística** (agrupa palavras por linha/coluna), não reconstrói grelhas complexas. **`pdf_extract_images`** grava XObjects (PNG quando `TryGetPng` devolve dados; caso contrário ficheiro `.bin` ou `.jpg` conforme `image_format`).

---

## Ações

| `type` | Descrição | PAD `action_id` |
|--------|-----------|-----------------|
| `pdf_extract_text` | Extrai texto para variável | `ExtractTextFromPDF` |
| `pdf_extract_tables` | Tabelas como JSON (lista de linhas, heurística) | `ExtractTablesFromPDF` |
| `pdf_extract_images` | Extrai imagens para pasta | `ExtractImagesFromPDF` |
| `pdf_extract_pages` | Escreve **novo PDF** só com páginas escolhidas | `ExtractPagesToNewPDF` |
| `pdf_merge` | Une vários PDFs num ficheiro | `MergePDFFiles` |

### Campos JSON (partilhados)

| Campo | Descrição |
|-------|------------|
| `path` | PDF principal (`{var}`; relativo ao projeto se não absoluto). Para `pdf_merge`, também pode ser o destino se `output_path` estiver vazio |
| `mode` | `all`, `page`, `range` (conforme operação; `pdf_extract_pages` usa `range` por defeito no Studio) |
| `page` | Página **1-based** quando `mode=page` |
| `start_page` / `end_page` | Intervalo **1-based** quando `mode=range` |
| `password` | Palavra-passe opcional |
| `output_path` | **Pasta** (`pdf_extract_images`), **ficheiro** novo (`pdf_extract_pages`), destino do **merge** (`pdf_merge`) |
| `input_paths` | **Uma linha por caminho** (`pdf_merge`) |
| `image_format` | `png` ou `jpg` (`pdf_extract_images`; sem conversão quando só há bytes crus) |
| `row_tolerance` | Número em pontos — tolerância vertical para agrupar palavras na mesma linha (`pdf_extract_tables`; vazio = 4) |
| `output_variable` | Obrigatório para texto/tabelas; opcional para merge/páginas (caminho do ficheiro) e imagens (JSON com lista de ficheiros) |

Implementação: `PdfContext` em `Hoblio.Runtime`.
