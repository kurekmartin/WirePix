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
        mainWindow.DialogClose(sender: this, result: null);
    }

    private void btnSendFeedback_Click(object sender, RoutedEventArgs e)
    {
        ShellLauncher.Open(target: "https://forms.gle/D5HTruypWjfqEToq6");
    }

    private void btnShowDumpFiles_Click(object sender, RoutedEventArgs e)
    {
        var crashRepFolder = Application.Current.Resources[key: Properties.Keys.CrashReportsFolder].ToString();

        var dir = new DirectoryInfo(path: crashRepFolder);
        FileInfo file = dir.GetFiles().OrderByDescending(keySelector: f => f.CreationTime).FirstOrDefault();
        if (file != null)
        {
            System.Diagnostics.Process.Start(fileName: "explorer.exe", arguments: "/select," + file.FullName);
        }
        else
        {
            System.Diagnostics.Process.Start(fileName: "explorer.exe", arguments: dir.FullName);
        }
    }
}