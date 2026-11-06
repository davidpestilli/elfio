using Hoblio.Model;

namespace Hoblio.Studio;

/// <summary>Paleta alinhada ao grupo PAD «Automação do navegador» (WebAutomation).</summary>
static class BrowserPaletteCatalog
{
    public static PaletteGroup Group => new()
    {
        Name = "Automação do navegador",
        Sections =
        [
            new PaletteSection
            {
                Name = "Extração de dados da Web",
                Items =
                [
                    Entry(WebAutomationActionTypes.ExtractTable, "Extrair tabela HTML", "ExtractData table"),
                    Entry(WebAutomationActionTypes.GetPageDetails, "Obter detalhes da página", "GetDetailsOfWebPage url title"),
                    Entry(WebAutomationActionTypes.GetElementDetails, "Obter detalhes do elemento", "GetDetailsOfElement attribute"),
                    Entry(WebAutomationActionTypes.GetText, "Capturar texto do elemento", "GetDetailsOfElement text"),
                    Entry(WebAutomationActionTypes.Screenshot, "Captura de tela da página", "TakeScreenshot png"),
                ],
            },
            new PaletteSection
            {
                Name = "Preenchimento de formulário",
                Items =
                [
                    Entry(WebAutomationActionTypes.Focus, "Focar campo de texto", "FocusBase focus input"),
                    Entry(WebAutomationActionTypes.Fill, "Preencher campo de texto", "PopulateTextField fill"),
                    Entry(WebAutomationActionTypes.SetCheckbox, "Definir caixa de seleção", "SetCheckboxState checkbox"),
                    Entry(WebAutomationActionTypes.SelectRadio, "Selecionar botão de opção", "SelectRadioButton radio"),
                    Entry(WebAutomationActionTypes.SetDropdown, "Definir lista suspensa", "SetDropDownListValue select"),
                    Entry(WebAutomationActionTypes.PressButton, "Pressionar botão", "PressButton submit"),
                    Entry(WebAutomationActionTypes.Upload, "Enviar arquivo para upload", "UploadFile upload file input arquivo anexo"),
                ],
            },
            new PaletteSection
            {
                Name = "Ações gerais",
                Items =
                [
                    Entry(WebAutomationActionTypes.WaitContent, "Aguardar conteúdo da página", "WaitForWebPageContent text element"),
                    Entry(WebAutomationActionTypes.LaunchChrome, "Iniciar Chrome", "LaunchChrome browser"),
                    Entry(WebAutomationActionTypes.LaunchEdge, "Iniciar Edge", "LaunchEdge browser"),
                    Entry(WebAutomationActionTypes.AttachChrome, "Anexar ao Chrome aberto", "AttachToChrome AttachToForegroundChrome ByUrl ByTitle"),
                    Entry(WebAutomationActionTypes.AttachEdge, "Anexar ao Edge aberto", "AttachToEdge AttachToTheForegroundEdge ByUrl ByTitle"),
                    Entry(WebAutomationActionTypes.Navigate, "Abrir URL (iniciar + navegar)", "LaunchChrome GoToWebPage navigate"),
                    Entry(WebAutomationActionTypes.GoTo, "Acessar página da Web", "GoToWebPage url"),
                    Entry(WebAutomationActionTypes.NewTab, "Criar nova guia", "CreateNewTab tab"),
                    Entry(WebAutomationActionTypes.SwitchPage, "Trocar de página (popup/aba)", "SwitchPage popup janela aba window.open"),
                    Entry(WebAutomationActionTypes.Click, "Clicar em link/elemento", "ClickBase click css"),
                    Entry(WebAutomationActionTypes.ClickDownload, "Clicar link de download", "ClickDownloadLink download"),
                    Entry(WebAutomationActionTypes.ExecuteScript, "Executar JavaScript", "ExecuteJavascript script"),
                    Entry(WebAutomationActionTypes.Hover, "Passar mouse sobre elemento", "HoverOverElement hover"),
                    Entry(WebAutomationActionTypes.WaitFor, "Esperar elemento", "WaitForElement selector"),
                    Entry(WebAutomationActionTypes.WaitNavigation, "Esperar navegação", "WaitForPageToLoad navigation"),
                    Entry(WebAutomationActionTypes.Reload, "Recarregar página", "ReloadWebPage refresh"),
                    Entry(WebAutomationActionTypes.Back, "Voltar", "NavigateBack history"),
                    Entry(WebAutomationActionTypes.Forward, "Avançar", "NavigateForward history"),
                    Entry(WebAutomationActionTypes.Close, "Fechar navegador", "CloseWebBrowser close"),
                ],
            },
        ],
    };

    static PaletteEntry Entry(string type, string title, string aliases) =>
        new($"action:{type}", title, aliases);
}
