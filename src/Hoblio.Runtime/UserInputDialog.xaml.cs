using System.Windows;
using Hoblio.Model;

namespace Hoblio.Runtime;

public partial class UserInputDialog : Window
{
    public string InputText { get; private set; } = "";

    public UserInputDialog(string title, string message, string defaultValue, UserInputKind kind, bool topmost)
    {
        InitializeComponent();
        Title = title;
        DataContext = new
        {
            DialogTitle = title,
            MessageText = message,
            Multiline = kind == UserInputKind.MultiLine,
            InputHeight = kind == UserInputKind.MultiLine ? 120.0 : 28.0,
        };
        InputBox.Text = defaultValue;
        if (topmost)
            Topmost = true;
    }

    void OkClick(object sender, RoutedEventArgs e)
    {
        InputText = InputBox.Text;
        DialogResult = true;
    }
}
