using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PhotoApp.Dialogs;

/// <summary>
/// Interaction logic for UpdateDialog.xaml
/// </summary>
public partial class UpdateDialog : UserControl
{
    private MainWindow mainWindow;
    public UpdateDialog(MainWindow window, Release release)
    {
        InitializeComponent();
        mainWindow = window;
        var version = Version.Parse(release.TagName.Replace("v", ""));
        ucUpdate.VersionInfo = $"{Properties.Resources.Update_NewVersionAvailable} ({version})";
        ucUpdate.Release = release;
        ucUpdate.DownloadingChanged += UcUpdate_DownloadingChanged;
    }

    private void UcUpdate_DownloadingChanged(object sender, EventArgs e)
    {
        if (ucUpdate.Downloading)
        {
            btnOK.IsEnabled = false;
        }
        else
        {
            btnOK.IsEnabled = true;
        }
    }

    private void btnOK_Click(object sender, RoutedEventArgs e)
    {
        mainWindow.DialogClose(this, null);
    }
}