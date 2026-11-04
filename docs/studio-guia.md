# Elfio Studio (C# / WPF + React Flow) — guia rápido

Interface inspirada no **Hoblio Studio (Rust/Tauri)** e no fluxo de trabalho do **Power Automate Desktop**: paleta à esquerda, **React Flow** ao centro (WebView2), propriedades à direita, log embaixo.

## Layout

| Área | Função |
|------|--------|
| **Faixa superior (WPF)** | **Elfio Studio**, Arquivo (Novo, Abrir, Salvar), Validar / Executar / Parar, abas de fluxo, status, nome do documento (`untitled *`) e pasta do projeto |
| **Barra do diagrama** | **Exibição / Edição**, desfazer/refazer, apagar seleção, limpar fluxo; com uma ligação selecionada, rótulo customizado da aresta |
| **Paleta** | Ações agrupadas por categoria; clique para inserir no fluxo |
| **Diagrama** | Nós e conexões (waypoints, reconectar handles); selecione um nó para editar |
| **Propriedades** | Sem nó: variáveis de projeto, índice **Flow variables**; com nó: campos por tipo de ação / loop / `run_flow` |
| **Log** | Saída da execução (texto ou JSON) |

## Fluxo típico

1. **Abrir** um projeto existente (`manifest.json` em `examples/…`) ou **Novo** (canvas vazio — adicione Start/End pela paleta **Utilitários**).
2. Ative **Edição** na toolbar do diagrama.
3. **Clique** na paleta para adicionar nós. **Ligue** arrastando dos **handles** de saída (`then`, `else`, `body`, `after`, …) para o handle de entrada do destino. **Delete** remove nós ou ligações selecionadas.
4. **Canvas (React Flow):** seleção múltipla, **Ctrl+C / X / V**, snap e guias de alinhamento, reconectar aresta arrastando a ponta, **panOnScroll** / Controls / MiniMap. Clique numa ligação para rótulo na toolbar ou waypoints (abaixo).
5. Ajuste **Propriedades** (tipo, parâmetros, condições de loop, **Run flow**). Em **HttpGet**, o campo **Variable** (opcional) guarda o corpo da resposta na execução.
6. **Validar** (também `{refs}` desconhecidas no C#) → **Executar** → **log**; **Parar** cancela (delay cooperativo).

## Modos Exibição / Edição

- **Exibição**: evita alterações acidentais; propriedades ficam somente leitura.
- **Edição**: desfazer/refazer, limpar fluxo, excluir nós, editar variáveis JSON.

## Dicas

- Use **Filtrar** na paleta para achar ações rapidamente.
- **Flow variables** (sem nó selecionado) lista produtores de variáveis; clique no nó para ir ao bloco.
- Portas `then` / `else` / `body` / `after` são escolhidas ao **ligar** no canvas ou ao **reconectar** a aresta.

## Subfluxos, manifest.flows e Run flow

- Abas de **arquivo** (`Principal`, `greet`, `finish`, …) vêm de `manifest.json` → `flows` (ex.: `examples/flow-compose-demo`).
- Abas **subflow_*** são fluxos embutidos no JSON ativo (`subflows` no `flow.json`).
- **Novo subfluxo** adiciona um bloco embutido no arquivo que você está editando.
- **Run flow** (paleta → Fluxos): use `flow_id` com combo dos IDs do manifest ou subfluxo embutido; `path` para JSON relativo.
- Salvar grava `flow.json` + cada `flows/*.json` alterado.

## Waypoints (tracado de conexão)

Comportamento alinhado ao **WaypointEdge** do Studio Rust (React Flow):

1. Clique na **linha da conexão** (modo Edição) — halo azul grosso indica seleção.
2. Sem waypoints manuais, a rota é **smooth-step**; aparece um **+** no meio do trecho lógico origem→destino (como no Rust).
3. Com waypoints, a rota vira **polilinha** reta entre origem, pontos e destino; **+** em cada segmento lógico.
4. Arraste os **pontos azuis** (duplo clique remove); handles acompanham **zoom/pan** do canvas.
5. Salve — persiste em `edge.ui.waypoints` no `flow.json`.

Paridade com o Studio Rust: [PARITY.md](PARITY.md).
