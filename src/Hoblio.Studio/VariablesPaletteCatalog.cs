using Hoblio.Model;

namespace Hoblio.Studio;

static class VariablesPaletteCatalog
{
    public static PaletteGroup Group => new()
    {
        Name = "Variáveis",
        Sections =
        [
            new PaletteSection
            {
                Name = "Variável escalar",
                Items = ScalarItems,
            },
            new PaletteSection
            {
                Name = "Tabela de dados",
                Items = DatatableItems,
            },
            new PaletteSection
            {
                Name = "Números",
                Items = NumberItems,
            },
            new PaletteSection
            {
                Name = "Listas",
                Items = ListItems,
            },
            new PaletteSection
            {
                Name = "JSON / objeto",
                Items = JsonItems,
            },
        ],
    };

    static IList<PaletteEntry> ScalarItems =>
    [
        new("action:set_variable", "Definir variável", "Assign set variable", "Define valor"),
        new("action:increase_variable", "Aumentar variável", "IncreaseVariable", "Incrementa número"),
        new("action:decrease_variable", "Diminuir variável", "DecreaseVariable", "Decrementa número"),
    ];

    static IList<PaletteEntry> DatatableItems =>
    [
        Entry(VariablesActionTypes.CreateDatatable, "Criar nova tabela de dados", "CreateNewDatatable"),
        Entry(VariablesActionTypes.AddRow, "Inserir linha na tabela", "AddRowToDataTable"),
        Entry(VariablesActionTypes.DeleteRow, "Excluir linha da tabela", "DeleteRowFromDataTable"),
        Entry(VariablesActionTypes.ModifyCell, "Atualizar item da tabela", "ModifyDataTableItem"),
        Entry(VariablesActionTypes.FindReplaceDatatable, "Encontrar ou substituir na tabela", "FindOrReplaceInDataTable"),
        Entry(VariablesActionTypes.AddColumn, "Inserir coluna na tabela", "AddColumnToDataTable"),
        Entry(VariablesActionTypes.DeleteColumn, "Excluir coluna da tabela", "DeleteColumnFromDataTable"),
        Entry(VariablesActionTypes.DeleteEmptyRows, "Excluir linhas vazias", "DeleteEmptyRowsFromDataTable"),
        Entry(VariablesActionTypes.DeleteDuplicateRows, "Excluir linhas duplicadas", "DeleteDuplicateRowsFromDataTable"),
        Entry(VariablesActionTypes.ClearDatatable, "Limpar tabela de dados", "ClearDataTable"),
        Entry(VariablesActionTypes.SortDatatable, "Classificar tabela", "SortDataTable"),
        Entry(VariablesActionTypes.FilterDatatable, "Filtrar tabela", "FilterDataTable"),
        Entry(VariablesActionTypes.MergeDatatables, "Mesclar tabelas", "MergeDataTables"),
        Entry(VariablesActionTypes.JoinDatatable, "Ingressar tabelas", "JoinDataTable"),
        Entry(VariablesActionTypes.ReadCsv, "Ler CSV para tabela", "GenerateDataTableFromCSV ReadCSV"),
        Entry(VariablesActionTypes.WriteCsv, "Converter tabela em CSV", "ConvertDataTableToCSV WriteCSV"),
        Entry(VariablesActionTypes.ColumnToList, "Coluna da tabela para lista", "RetrieveDataTableColumnIntoList"),
    ];

    static IList<PaletteEntry> NumberItems =>
    [
        Entry(VariablesActionTypes.TruncateNumber, "Número truncado", "TruncateNumber RoundNumber"),
        Entry(VariablesActionTypes.RandomNumber, "Gerar número aleatório", "GenerateRandomNumber RandomNumber"),
    ];

    static IList<PaletteEntry> ListItems =>
    [
        new("action:list_clear", "Limpar lista", "ClearList list clear", "Esvazia lista"),
        new("action:list_append", "Adicionar item à lista", "AddItemToList list append", "Acrescenta item"),
        Entry(VariablesActionTypes.CreateList, "Criar lista", "CreateNewList"),
        Entry(VariablesActionTypes.RemoveListItem, "Remover item da lista", "RemoveItemFromList"),
        Entry(VariablesActionTypes.SortList, "Classificar lista", "SortList SortListByProperty"),
        Entry(VariablesActionTypes.ShuffleList, "Lista aleatória", "ShuffleList"),
        Entry(VariablesActionTypes.MergeLists, "Mesclar listas", "MergeLists"),
        Entry(VariablesActionTypes.ReverseList, "Lista reversa", "ReverseList"),
        Entry(VariablesActionTypes.RemoveDuplicateListItems, "Remover duplicados da lista", "RemoveDuplicateItemsFromList"),
        Entry(VariablesActionTypes.FindCommonListItems, "Itens comuns entre listas", "FindCommonListItems"),
        Entry(VariablesActionTypes.SubtractLists, "Subtrair listas", "SubtractLists"),
        Entry(VariablesActionTypes.FilterList, "Filtrar lista", "FilterList"),
    ];

    static IList<PaletteEntry> JsonItems =>
    [
        Entry(VariablesActionTypes.JsonToCustomObject, "Converter JSON em objeto", "ConvertJsonToCustomObject"),
        Entry(VariablesActionTypes.CustomObjectToJson, "Converter objeto em JSON", "ConvertCustomObjectToJson"),
    ];

    static PaletteEntry Entry(string type, string title, string searchAliases, string? hint = null) =>
        new($"action:{type}", title, hint, searchAliases);
}
