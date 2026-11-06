# Módulo HTTP (`http_get`, `http_request`)

Requisições **HTTP/HTTPS sem navegador** (paridade conceitual com o grupo **HTTP** do PAD e a ação **Invocar serviço Web**). Telemetria: módulo **`Web`**, `action_id` **`InvokeWebServiceBase`**.

**Implementação:** `Hoblio.Runtime` (`ActionExecutor`), `HttpClient` compartilhado por processo.

---

## `http_get`

| Campo JSON | Descrição |
|------------|-----------|
| `url` | URL (interpolação `{var}`) |
| `variable` | Opcional: nome da variável para o corpo da resposta |

---

## `http_request` (REST)

| Campo JSON | Descrição |
|------------|-----------|
| `url` | URL (obrigatório; `{var}`) |
| `method` | `get`, `post`, `put`, `patch`, `delete`, `head`, `options`, `connect`, `trace` (padrão `get`) |
| `accept` | Opcional: cabeçalho `Accept` |
| `body` | Opcional: corpo; interpola `{var}` (ex. JSON com `{Result}`) |
| `content_type` | Opcional: `Content-Type` quando há corpo; se omitido com corpo, usa `application/json; charset=utf-8` |
| `custom_headers` | Opcional: uma linha por cabeçalho `Nome: valor` (linhas com `#` no início são ignoradas). `Content-Type` aqui aplica-se ao corpo se `content_type` estiver vazio |
| `response_body_variable` | Opcional: variável com corpo da resposta |
| `response_headers_variable` | Opcional: variável com cabeçalhos (texto, um por linha) |
| `status_code_variable` | Opcional: variável com código HTTP (string, ex. `200`) |

Cancelamento: a requisição respeita o token de cancelamento da execução do fluxo (parar run).

---

## Diferença em relação ao PAD

- **Autenticação OAuth / certificado cliente / proxy corporativo** — fora do escopo atual (ver produto/Hoblio decisions).
- **Download para ficheiro / SOAP** — não implementado; usar `file_write` + corpo em variável, ou expandir o módulo depois.

Ver também [catalogo-acoes-e-modulos.md](./catalogo-acoes-e-modulos.md).
