using System.Windows;
using System.Windows.Controls;

namespace PhotoApp.Dialogs;

/// <summary>
/// Interakční logika pro ErrorDialog.xaml
/// </summary>
public partial class ErrorDialog : UserControl
{
    private MainWindow mainWindow;
    public ErrorDialog(MainWindow window, string msg)
    {
        InitializeComponent();
        mainWindow = window;
        lblMessage.Text = msg;
    }

    private void btnOK_Click(object sender, RoutedEventArgs e)
    {
        mainWindow.DialogClose(this, null);
    }
}