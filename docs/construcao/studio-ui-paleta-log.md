# Studio — melhorias de UI (paleta, sidebars, log)

Registro técnico das alterações na interface WPF + React Flow (WebView2).

## Paleta de ações

| Artefato | Responsabilidade |
|----------|------------------|
| `ActionPalettePanel.xaml` | Layout: busca, banner read-only, expanders, cards |
| `ActionPaletteCatalog.cs` | Grupos, rótulos PT, `SearchAliases` |
| `PaletteVisuals.cs` | Cores/ícones por `Key` (paridade com `flow-canvas/styles.css`) |
| `PaletteSearch.cs` | Índice e match de filtro |
| `PaletteGroupViewModel.cs` | `StartsExpanded`, `HeaderText`, VM de entrada |
| `StudioTheme.xaml` | `PaletteSection`, `PaletteSearchBox`, `PaletteActionButton`, … |

Correção de startup: `PaletteSearchBox` não usa mais `BasedOn={StaticResource FieldBox}` (ordem no `ResourceDictionary` quebrava o parse XAML).

## Sidebars fechados ao abrir

- Paleta: removido `DefaultExpanded` dos grupos; `RebuildFilteredPalette` só expande grupos quando há texto na busca.
- `PropertiesPanel.xaml`: `IsExpanded="False"` nos expanders; removido auto-expand de Flow variables.
- `ProjectVariablesPanel.xaml`: JSON avançado explícito fechado.

## Log de execução

| Artefato | Responsabilidade |
|----------|------------------|
| `RunLogFormatter.cs` | Eventos → linhas PT, profundidade de subfluxo, `NodeId` |
| `RunLogLine.cs` | Cores, indent, `CanFocusNode` |
| `FlowEditorViewModel.RunLog.cs` | `FocusLogLineNodeCommand`, `PulseLogToolbarNotice` |
| `MainWindow.xaml` | Lista, `LogLineButton`, hover, toolbar em Grid |
| `RunLogListBoxItem` | Sem highlight amarelo de seleção WPF |

Bridge canvas:

- `FlowCanvasHost.FlashNode` → `{ type: "flashNode", payload: { nodeId, durationMs } }`
- `App.tsx`: `setCenter`, flag `isFlashing` no nó
- `FlowNode.tsx` / `styles.css`: classe `.flash` (animação âmbar)

## Testes

- `PaletteSearchTests.cs` — aliases e query vazia
- `RunLogLineTests.cs` — módulo, `NodeId`, profundidade, `node_exit` oculto

## Shell — título da janela e nome do projeto

| Comportamento | Implementação |
|---------------|---------------|
| Marca **Elfio Studio** | Texto + ícone no ribbon (`MainWindow.xaml`); não repetir na moldura OS. |
| Título da janela | `Title="{Binding DocumentTitle}"` — mesmo rótulo do canto direito do ribbon. |
| `DocumentTitle` | `ProjectDisplayName()` + ` *` se sujo; nome vem de `manifest.name`, com fallback para **nome da pasta** quando `name` é vazio ou `untitled`. |
| Caminho | `ProjectPath` abaixo do status (pasta do `manifest.json` aberto). |

## Canvas — Switch / Case (handles)

| Artefato | Nota |
|----------|------|
| `FlowCanvasHandles.cs` | Switch: porta lógica `then`, handle visual `right`, label de aresta “cases”. |
| `nodeLayout.ts` | Linha lógica de conectores (`HOBLIO_ROW_Y`); top/bottom no padrão XYFlow. |
| `FlowNode.tsx` / `styles.css` | Faixa compacta de título/portas; metadados abaixo sem deslocar handles horizontais. |

Rebuild do canvas após mudanças TS: `npm run build --prefix src/Hoblio.Studio/flow-canvas`.

## Build

```bash
npm run build --prefix src/Hoblio.Studio/flow-canvas
dotnet run --project src/Hoblio.Studio
```
