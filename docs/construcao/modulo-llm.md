# Módulo LLM (`llm_completion`)

Chamada **HTTP POST** com corpo JSON gerado para servidores **Ollama** (`/api/chat`) ou APIs estilo **OpenAI** (`/v1/chat/completions`). Telemetria: módulo **`Web`**, `action_id` **`InvokeLLM`** (paridade conceitual com PAD **Invoke LLM** / fluxo local Ollama ou LM Studio).

## Campos JSON

| Campo | Descrição |
|-------|------------|
| `url` | URL completa do endpoint (ex.: `http://127.0.0.1:11434/api/chat` ou `http://127.0.0.1:1234/v1/chat/completions` no LM Studio) |
| `api_style` | `ollama_chat` (predefinição) ou `openai_chat` |
| `model` | Nome do modelo (`{var}` permitido) |
| `system` | Mensagem de sistema (opcional) |
| `user` | Instrução principal / utilizador |
| `document` | Texto longo ou contexto (opcional); concatenado ao `user` com separador |
| `json_schema` | Ollama: envia-se como `format` (use `json` ou JSON de esquema). OpenAI: `json_object` ou JSON para `response_format` |
| `temperature` | Opcional, número em texto |
| `response_body_variable` | Variável com o **corpo bruto** da resposta (JSON do servidor) |

**Requisito:** pelo menos uma de `system`, `user` ou `document` deve ficar não vazia após interpolação.

Implementação: `ActionExecutor.LlmCompletion` em `Hoblio.Runtime` (usa `HttpClient` partilhado).

## Ligação com PDF / HTTP

Fluxo típico: `pdf_extract_text` → variável → `llm_completion` com `document: "{PdfText}"` e URL do Ollama; ou `http_request` manual se precisar de corpo totalmente personalizado.
