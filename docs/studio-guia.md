# Hoblio Studio (C# / WPF) — guia rápido

Interface inspirada no **Hoblio Studio (Rust)** e no fluxo de trabalho do **Power Automate Desktop**: paleta à esquerda, diagrama ao centro, propriedades à direita, log embaixo.

## Layout

| Área | Função |
|------|--------|
| **Faixa superior** | Arquivo (Novo, Abrir, Salvar) e fluxo (Validar, Executar, Parar) |
| **Paleta** | Ações agrupadas por categoria; clique para inserir no fluxo |
| **Diagrama** | Nós e conexões; selecione um nó para editar |
| **Propriedades** | Campos do nó selecionado, variáveis do projeto, conexões avançadas |
| **Log** | Saída da execução (texto ou JSON) |

## Fluxo típico

1. **Abrir** um projeto existente (`manifest.json` em `examples/…`) ou trabalhar no demo inicial.
2. Ative **Edição** na barra acima do diagrama.
3. **Clique** na paleta para adicionar nós. **Ligue** arrastando do conector de saída (direita) para o de entrada (esquerda). **Desligue** com Alt+clique no conector ou **Delete** com a linha selecionada. Em **Conexões** (painel direito) dá para mudar porta, destino ou criar arestas por id.
4. **Canvas:** arraste em área vazia para **selecionar vários nós** (retângulo azul); **Ctrl+clique** soma/remove da seleção; arraste um nó selecionado para **mover o grupo**. **Ctrl+C / Ctrl+X / Ctrl+V** copia, recorta ou cola a seleção (inclui arestas entre nós selecionados). Ligue nós pelos **conectores** ou use **Nova ligação** no painel (sem nó selecionado); clique numa ligação para editar **To** / **Port** ou remover. Campos de texto com variáveis `{nome}` têm lista de inserção rápida abaixo do campo. **Espaço+arrastar**, **botão do meio** ou **botão direito** movem o viewport (pan). **Ctrl+roda** faz zoom.
5. **Arestas:** roteamento **smooth-step** (como React Flow / Studio Rust) — cantos arredondados, saída à direita e entrada à esquerda; waypoints manuais usam polilinha reta.
4. Ajuste **Propriedades** (tipo, parâmetros, condições de loop).
5. **Validar** → **Executar** → acompanhe o **log** (**Copiar log** / toggle JSON); use **Parar** para cancelar.

## Modos Exibição / Edição

- **Exibição**: evita alterações acidentais; propriedades ficam somente leitura.
- **Edição**: desfazer/refazer, limpar fluxo, excluir nós, editar variáveis JSON.

## Dicas

- Use **Filtrar** na paleta para achar ações rapidamente.
- **Variáveis produzidas** (painel recolhido) lista saídas de ações no fluxo.
- **Conexões (avançado)** permite ajustar portas `then` / `else` / `body` / `after` em fluxos com If e loops.

## Subfluxos, manifest.flows e Run flow

- Abas de **arquivo** (`Principal`, `greet`, `finish`, …) vêm de `manifest.json` → `flows` (ex.: `examples/flow-compose-demo`).
- Abas **subflow_*** são fluxos embutidos no JSON ativo (`subflows` no `flow.json`).
- **Novo subfluxo** adiciona um bloco embutido no arquivo que você está editando.
- **Run flow** (paleta → Fluxos): use `flow_id` com combo dos IDs do manifest ou subfluxo embutido; `path` para JSON relativo.
- Salvar grava `flow.json` + cada `flows/*.json` alterado.

## Waypoints (tracado de conexão)

1. Clique na **linha da conexão** no diagrama (modo Edição).
2. Arraste os **pontos azuis**; **duplo clique** remove; botões **+** nos segmentos inserem pontos.
3. **+ Waypoint** na barra ainda adiciona um ponto no meio (atalho).
4. Salve — persiste em `edge.ui.waypoints`.

Paridade com o Studio Rust: [PARITY.md](PARITY.md).
