namespace Hoblio.Model;

public static class WebAutomationEnums
{
    public static class BrowserEngine
    {
        public const string Chrome = "chrome";
        public const string Edge = "edge";
    }

    public static class PopulateMode
    {
        public const string Replace = "replace";
        public const string Append = "append";
    }

    public static class ElementDetail
    {
        public const string Text = "text";
        public const string Value = "value";
        public const string Href = "href";
        public const string InnerHtml = "inner_html";
        public const string OuterHtml = "outer_html";
        public const string Checked = "checked";
        public const string TagName = "tag_name";
    }

    public static class PageDetail
    {
        public const string Url = "url";
        public const string Title = "title";
        public const string All = "all";
    }

    public static class WaitContentMode
    {
        public const string ContainsText = "contains_text";
        public const string NotContainsText = "not_contains_text";
        public const string ContainsElement = "contains_element";
        public const string NotContainsElement = "not_contains_element";
    }

    public static class DropdownMode
    {
        public const string ByText = "by_text";
        public const string ByIndex = "by_index";
        public const string ByValue = "by_value";
    }

    public static class AttachMode
    {
        public const string ByUrl = "by_url";
        public const string ByTitle = "by_title";
        public const string Foreground = "foreground";
    }

    /// <summary>Como reagir a um <c>confirm()</c>/<c>alert()</c> aberto pela página durante o clique.</summary>
    public static class DialogAction
    {
        public const string Accept = "accept";
        public const string Dismiss = "dismiss";
    }
}
