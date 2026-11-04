# Renomear pasta do repositório para `elfio`

O Vite não compila o `flow-canvas` quando o caminho contém `#` (ex.: `hoblio-c#`).

1. Feche o Cursor/IDE e terminais que usem o repositório.
2. No PowerShell:

```powershell
Move-Item -LiteralPath "E:\Projetos\hoblio-c#" -Destination "E:\Projetos\elfio"
```

3. Reabra a pasta `E:\Projetos\elfio` no Cursor.
4. Build direto passa a funcionar:

```powershell
cd E:\Projetos\elfio\src\Hoblio.Studio\flow-canvas
npm run build
dotnet build
```

Enquanto a pasta ainda for `hoblio-c#`, use `.\scripts\build-flow-canvas.ps1` (copia para `%TEMP%` antes do Vite).
