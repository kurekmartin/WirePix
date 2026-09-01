using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PhotoApp.Dialogs;

/// <summary>
/// Interakční logika pro ErrorDialog.xaml
/// </summary>
public partial class AppFeedbackDialog : UserControl
{
    private MainWindow mainWindow;
    public AppFeedbackDialog(MainWindow window)
    {
        InitializeComponent();
        mainWindow = window;
    }

    private void btnOK_Click(object sender, RoutedEventArgs e)
    {
        mainWindow.DialogClose(this, null);
    }
    private void btnSendFeedback_Click(object sender, RoutedEventArgs e)
    {
        ShellLauncher.Open("https://forms.gle/D5HTruypWjfqEToq6");
    }

    private void btnShowDumpFiles_Click(object sender, RoutedEventArgs e)
    {
        string crashRepFolder = Application.Current.Resources[Properties.Keys.CrashReportsFolder].ToString();

        var dir = new DirectoryInfo(crashRepFolder);
        var file = dir.GetFiles().OrderByDescending(f => f.CreationTime).FirstOrDefault();
        if (file != null)
        {
            System.Diagnostics.Process.Start("explorer.exe", "/select," + file.FullName);
        }
        else
        {
            System.Diagnostics.Process.Start("explorer.exe", dir.FullName);
        }

    }
}