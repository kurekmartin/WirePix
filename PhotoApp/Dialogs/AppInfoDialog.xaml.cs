using Octokit;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace PhotoApp.Dialogs;

/// <summary>
/// Interakční logika pro ErrorDialog.xaml
/// </summary>
public partial class AppInfoDialog : UserControl
{
    private MainWindow mainWindow;

    public AppInfoDialog(MainWindow window)
    {
        InitializeComponent();
        mainWindow = window;
    }

    private async void btnCheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        MaterialDesignThemes.Wpf.ButtonProgressAssist.SetIsIndicatorVisible(element: btnCheckUpdate, isIndicatorVisible: true);
        Version currentVersion = Version.Parse(input: App.Instance.Version);

        try
        {
            var github = new GitHubClient(productInformation: new ProductHeaderValue(name: "WirePix"));
            Release release = await github.Repository.Release.GetLatest(owner: "KurekMartin", name: "WirePix");
            Version latestVersion = Version.Parse(input: release.TagName.Replace(oldValue: "v", newValue: ""));

            if (latestVersion > currentVersion)
            {
                ucUpdate.VersionInfo = $"{Properties.Resources.Update_NewVersionAvailable} ({latestVersion})";
                ucUpdate.Release = release;
            }
            else
            {
                ucUpdate.VersionInfo = Properties.Resources.Update_NoNewVerison;
            }
        }
        catch
        {
            ucUpdate.VersionInfo = Properties.Resources.Update_Error;
        }

        MaterialDesignThemes.Wpf.ButtonProgressAssist.SetIsIndicatorVisible(element: btnCheckUpdate, isIndicatorVisible: false);
    }

    private void ucUpdate_DownloadingChanged(object sender, EventArgs e)
    {
        btnCheckUpdate.IsEnabled = btnOK.IsEnabled = !ucUpdate.Downloading;
    }

    private void btnShowLicense_Click(object sender, RoutedEventArgs e)
    {
        ShellLauncher.Open(target: "https://github.com/KurekMartin/WirePix/blob/master/LICENSE");
    }

    private void btnShowCode_Click(object sender, RoutedEventArgs e)
    {
        ShellLauncher.Open(target: "https://github.com/KurekMartin/WirePix");
    }

    private void tbEmail_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Clipboard.SetText(text: tbEmail.Text);
        SnackBar.MessageQueue.Enqueue(content: Properties.Resources.EmailCopied);
    }

    private void btnLibraries_Click(object sender, RoutedEventArgs e)
    {
        mainWindow.ShowLibraries(sender: this);
    }

    private void btnShowChangelog_Click(object sender, RoutedEventArgs e)
    {
        mainWindow.ShowChangelog(sender: this);
    }

    private void btnShowAppdata_Click(object sender, RoutedEventArgs e)
    {
        var dataFolder = System.Windows.Application.Current.Resources[key: Properties.Keys.MainFolder].ToString();
        Process.Start(fileName: "explorer.exe", arguments: dataFolder);
    }
}
