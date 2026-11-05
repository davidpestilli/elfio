using Hoblio.Model;

namespace Hoblio.Studio;

static class WordPaletteCatalog
{
    public static PaletteGroup Group => new()
    {
        Name = "Word",
        DefaultExpanded = true,
        Items =
        [
            Entry(WordActionTypes.Launch, "Iniciar o Word", "LaunchWord Launch", "Novo documento ou abrir ficheiro"),
            Entry(WordActionTypes.Attach, "Anexar ao Word em execução", "AttachToRunningWord Attach", "Conecta a documento aberto"),
            Entry(WordActionTypes.Save, "Salvar o Word", "SaveWord Save", "Salvar ou Salvar como"),
            Entry(WordActionTypes.Close, "Fechar o Word", "CloseWord Close", "Fecha documento ou aplicação"),
            Entry(WordActionTypes.Read, "Ler o documento do Word", "ReadFromWord Read", "Documento, páginas ou marcador"),
            Entry(WordActionTypes.Write, "Escrever em documento do Word", "WriteToWord Write", "Início, fim ou marcador"),
            Entry(WordActionTypes.InsertImage, "Inserir imagem em documento do Word", "InsertImageToWord", "Ficheiro ou área de transferência"),
            Entry(WordActionTypes.FindReplace, "Localizar e substituir palavras em documento do Word", "FindAndReplaceWord", "Busca e substitui"),
        ],
    };

    static PaletteEntry Entry(string type, string title, string aliases, string hint) =>
        new($"action:{type}", title, aliases, hint);
}
