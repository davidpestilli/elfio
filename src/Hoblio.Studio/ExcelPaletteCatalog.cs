using Hoblio.Model;

namespace Hoblio.Studio;

static class ExcelPaletteCatalog
{
    public static PaletteGroup Group => new()
    {
        Name = "Excel",
        DefaultExpanded = true,
        Sections =
        [
            new PaletteSection
            {
                Name = "Básico",
                DefaultExpanded = true,
                Items = BasicItems,
            },
            new PaletteSection
            {
                Name = "Avançado",
                DefaultExpanded = false,
                Items = AdvancedItems,
            },
        ],
    };

    static IList<PaletteEntry> BasicItems =>
    [
        Entry(ExcelActionTypes.Launch, "Iniciar o Excel", "LaunchExcel Launch", "Inicia ou abre pasta de trabalho"),
        Entry(ExcelActionTypes.Attach, "Anexar ao Excel em execução", "AttachToRunningExcel Attach", "Conecta a instância aberta"),
        Entry(ExcelActionTypes.Read, "Ler da planilha do Excel", "ReadFromExcel Read", "Lê células ou intervalo"),
        Entry(ExcelActionTypes.GetActiveCell, "Obter célula ativa", "GetActiveCell", "Endereço da célula ativa"),
        Entry(ExcelActionTypes.Save, "Salvar o Excel", "SaveExcel Save", "Salvar ou Salvar como"),
        Entry(ExcelActionTypes.Write, "Gravar na planilha do Excel", "WriteToExcel Write", "Escreve valor em célula"),
        Entry(ExcelActionTypes.Close, "Fechar o Excel", "CloseExcel Close", "Fecha instância"),
        Entry(ExcelActionTypes.SetActiveWorksheet, "Definir planilha ativa", "SetActiveWorksheet", "Ativa planilha por nome ou índice"),
        Entry(ExcelActionTypes.AddWorksheet, "Adicionar nova planilha", "AddWorksheet", "Insere planilha"),
        Entry(ExcelActionTypes.GetFirstFreeColumnRow, "Obter primeira coluna/linha livre", "GetFirstFreeColumnRow", "Próxima célula livre"),
        Entry(ExcelActionTypes.GetColumnName, "Obter nome da coluna", "GetColumnName", "Letra da coluna por índice"),
        Entry(ExcelActionTypes.ClearCells, "Limpar células", "ClearCellsFromExcel Clear", "Limpa conteúdo"),
        Entry(ExcelActionTypes.SortCells, "Classificar células", "SortCellsFromExcel Sort", "Ordena intervalo"),
        Entry(ExcelActionTypes.FilterCells, "Filtrar células", "FilterCellsFromExcel Filter", "AutoFiltro"),
        Entry(ExcelActionTypes.ClearFilter, "Limpar filtros", "ClearFilterFromExcel", "Remove filtros"),
        Entry(ExcelActionTypes.GetEmptyCell, "Obter célula vazia", "GetEmptyCellFromExcel", "Próxima célula vazia"),
    ];

    static IList<PaletteEntry> AdvancedItems =>
    [
        Entry(ExcelActionTypes.ResizeColumnsOrRows, "Redimensionar colunas/linhas", "ResizeColumnsOrRows", "Largura ou altura"),
        Entry(ExcelActionTypes.RunMacro, "Executar macro do Excel", "RunExcelMacro RunMacro", "Executa macro VBA"),
        Entry(ExcelActionTypes.GetActiveWorksheet, "Obter planilha ativa", "GetActiveWorksheet", "Nome da planilha ativa"),
        Entry(ExcelActionTypes.GetAllWorksheets, "Obter todas as planilhas", "GetAllWorksheets", "Lista de planilhas"),
        Entry(ExcelActionTypes.DeleteWorksheet, "Excluir planilha", "DeleteWorksheet", "Remove planilha"),
        Entry(ExcelActionTypes.RenameWorksheet, "Renomear planilha", "RenameWorksheet", "Altera nome"),
        Entry(ExcelActionTypes.CopyWorksheet, "Copiar planilha", "CopyExcelWorksheet", "Duplica planilha"),
        Entry(ExcelActionTypes.ActivateCell, "Ativar célula", "ActivateCellInExcel", "Seleciona célula ativa"),
        Entry(ExcelActionTypes.SelectCells, "Selecionar células", "SelectCellsFromExcel", "Seleciona intervalo"),
        Entry(ExcelActionTypes.GetSelectedRange, "Obter intervalo selecionado", "GetSelectedCellRange", "Endereço selecionado"),
        Entry(ExcelActionTypes.CopyCells, "Copiar células", "CopyCellsFromExcel", "Copia para área de transferência"),
        Entry(ExcelActionTypes.PasteCells, "Colar células", "PasteCellsToExcel", "Cola do clipboard"),
        Entry(ExcelActionTypes.DeleteCells, "Excluir da planilha", "DeleteCells", "Exclui células"),
        Entry(ExcelActionTypes.InsertRow, "Inserir linha", "InsertRow", "Insere linha"),
        Entry(ExcelActionTypes.DeleteRow, "Excluir linha", "DeleteRow", "Remove linha"),
        Entry(ExcelActionTypes.InsertColumn, "Inserir coluna", "InsertColumn", "Insere coluna"),
        Entry(ExcelActionTypes.DeleteColumn, "Excluir coluna", "DeleteColumn", "Remove coluna"),
        Entry(ExcelActionTypes.FindReplace, "Localizar e substituir", "FindAndReplace", "Busca e substitui"),
        Entry(ExcelActionTypes.GetFirstFreeRowOnColumn, "Obter primeira linha livre na coluna", "GetFirstFreeRowOnColumn", "Linha livre em coluna"),
        Entry(ExcelActionTypes.ReadCellFormula, "Ler fórmula", "ReadCellFormula", "Fórmula da célula"),
        Entry(ExcelActionTypes.GetTableRange, "Obter intervalo de tabelas", "GetTableRange", "Intervalo de tabela"),
        Entry(ExcelActionTypes.AutofillCells, "Preencher automaticamente", "AutoFillCells Autofill", "Auto preenchimento"),
        Entry(ExcelActionTypes.AppendCells, "Acrescentar células", "AppendCells", "Anexa dados"),
        Entry(ExcelActionTypes.LookupRange, "Intervalo de pesquisa", "LookupRange", "Procura em intervalo"),
        Entry(ExcelActionTypes.SetColor, "Definir cor das células", "SetColor", "Cor de preenchimento"),
    ];

    static PaletteEntry Entry(string jsonType, string title, string searchAliases, string? hint) =>
        new($"action:{jsonType}", title, hint, searchAliases);
}
