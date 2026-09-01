using System.Windows;
using System.Windows.Controls;

namespace PhotoApp.Dialogs;

/// <summary>
/// Interakční logika pro YesNoDialog.xaml
/// </summary>
public partial class YesNoDialog : UserControl
{
    private int _requestCode;
    public static int RESULT_YES = 1;
    public static int RESULT_NO = 0;
    private MainWindow _mainWindow;

    public YesNoDialog(MainWindow window, string text, int requestCode)
    {
        InitializeComponent();
        tbMain.Text = text;
        _requestCode = requestCode;
        _mainWindow = window;
    }

    private void btnYes_Click(object sender, RoutedEventArgs e)
    {
        _mainWindow.DialogClose(sender: this, result: RESULT_YES, resultCode: MainWindow.RESULT_OK, requestCode: _requestCode);
    }
}