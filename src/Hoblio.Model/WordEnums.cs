namespace Hoblio.Model;

/// <summary>Valores aceitos em campos string de <see cref="WordAction"/> (modos PAD).</summary>
public static class WordEnums
{
    public static class ReadMode
    {
        public const string Document = "document";
        public const string Pages = "pages";
        public const string Bookmark = "bookmark";
    }

    public static class WriteMode
    {
        public const string Start = "start";
        public const string End = "end";
        public const string BookmarkStart = "bookmark_start";
        public const string BookmarkEnd = "bookmark_end";
    }

    public static class CloseMode
    {
        public const string Close = "close";
        public const string Save = "save";
        public const string SaveAs = "save_as";
    }

    public static class ImageSource
    {
        public const string File = "file";
        public const string Clipboard = "clipboard";
    }

    public static class InsertImageMode
    {
        public const string DocumentStart = "document_start";
        public const string DocumentEnd = "document_end";
        public const string BookmarkStart = "bookmark_start";
        public const string BookmarkEnd = "bookmark_end";
        public const string TextStart = "text_start";
        public const string TextEnd = "text_end";
    }
}
