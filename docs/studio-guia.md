# Elfio Studio (C# / WPF + React Flow) — guia rápido

Interface inspirada no **Hoblio Studio (Rust/Tauri)** e no fluxo de trabalho do **Power Automate Desktop**: paleta à esquerda, **React Flow** ao centro (WebView2), propriedades à direita, log embaixo.

## Layout

| Área | Função |
|------|--------|
| **Faixa superior (WPF)** | **Elfio Studio**, Arquivo (Novo, Abrir, Salvar), Validar / Executar / Parar, abas de fluxo, status, nome do documento (`untitled *`) e pasta do projeto |
| **Barra do diagrama** | **Exibição / Edição**, desfazer/refazer, apagar seleção, limpar fluxo; com uma ligação selecionada, rótulo customizado da aresta |
| **Paleta** | Ações por categoria (grupos colapsáveis); busca; clique para inserir no fluxo |
| **Diagrama** | Nós e conexões (waypoints, reconectar handles); selecione um nó para editar |
| **Propriedades** | Sem nó: variáveis de projeto, índice **Flow variables**; com nó: campos por tipo de ação / loop / `run_flow` |
| **Log** | Linhas formatadas em PT, navegação para o nó no canvas; opcional JSON bruto |

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

## Paleta de ações (sidebar esquerda)

- **Busca** com placeholder (sem label “Filtrar”); aceita português, id do nó e aliases em inglês (`HttpGet`, `Delay`, …). Com texto na busca, os grupos com resultados **abrem** automaticamente.
- **Grupos colapsáveis** (`Expander`) com contagem de itens. O grupo **Excel** contém subgrupos **Básico** e **Avançado** (expanders aninhados). Demais grupos seguem a política de expansão do catálogo (`DefaultExpanded` por grupo; Excel aberto, Avançado fechado por padrão).
- Cada ação é um **card**: faixa de cor alinhada ao canvas, ícone, título em PT, descrição curta; tooltip com detalhe.
- Instruções de uso no ícone **ⓘ** no cabeçalho (não ocupam espaço fixo).
- **Modo Exibição** ou execução em andamento: banner na paleta e botões de inserção desabilitados.

Metadados: `ActionPaletteCatalog.cs`, `ExcelPaletteCatalog.cs`, apresentação `PaletteVisuals.cs`, filtro `PaletteSearch.cs`. Excel: [construcao/modulo-excel.md](construcao/modulo-excel.md).

## Painéis laterais — estado inicial

- **Paleta** e **Propriedades** (incl. Flow variables, JSON avançado em variáveis de projeto): seções **recolhidas** ao entrar no Studio.
- A seção Flow variables **não** expande mais sozinha quando há produtores.

## Log de execução

- **Formato legível** (`RunLogFormatter`): textos em português, indentação por subfluxo, `node_exit` oculto; ações com módulo/nó; falhas em destaque vermelho.
- **Tema escuro**: linhas em cards `#2A2D33`, sem highlight amarelo do `ListBox`; scroll automático para a última linha.
- **Copiar log** / **Limpar**: aviso verde na barra do log (~4 s) e mensagem na barra de status; Limpar só habilitado se houver linhas.
- **Mostrar JSON**: checkbox à **direita** da barra (separado dos botões); alterna para JSONL por linha.
- **Clique na linha** (quando há nó associado): seleciona o nó, centraliza o canvas e **pisca** a borda (mensagem `flashNode` no React Flow). Se o nó não estiver no diagrama atual (ex. evento só no subfluxo), aviso na status bar.
- **Hover** em linhas clicáveis: fundo/borda levemente mais claros.

Build do canvas após alterar TypeScript: `npm run build` em `src/Hoblio.Studio/flow-canvas` (o `.csproj` copia `dist/` na compilação).

## Dicas

- Use a **busca** na paleta (português ou nomes em inglês, ex. `HttpGet`) para achar ações rapidamente.
- No **log**, clique numa linha de ação ou entrada de nó para localizar o bloco no diagrama.
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
