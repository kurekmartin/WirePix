using Octokit;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PhotoApp.UIElements;

public partial class UpdateControl : UserControl, INotifyPropertyChanged
{
    private string _versionInfo = "";
    private string _error = "";
    private Release _release;

    public Release Release
    {
        set
        {
            if (value != null)
            {
                _release = value;
                spDownloadUpdate.Visibility = Visibility.Visible;
            }
        }
    }

    public string VersionInfo
    {
        get => _versionInfo;
        set
        {
            _versionInfo = value;
            OnPropertyChanged();
        }
    }

    public string Error
    {
        get => _error;
        set
        {
            if (value != null)
            {
                _error = value;
                OnPropertyChanged();
            }
        }
    }

    public bool Downloading { get; private set; } = false;
    private static readonly string _tmpFolder = System.Windows.Application.Current.Resources[key: Properties.Keys.TempFolder].ToString();
    private string downloadFile;
    private WebClient WebClient = new();

    public event PropertyChangedEventHandler PropertyChanged;
    public event EventHandler DownloadingChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(sender: this, e: new PropertyChangedEventArgs(propertyName: propertyName));
    }

    public UpdateControl()
    {
        InitializeComponent();
        DataContext = this;
        WebClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
    }

    private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
    {
        if (e.Cancelled)
        {
            var file = new FileInfo(fileName: downloadFile);
            if (file.Exists)
            {
                file.Delete();
            }
        }
    }

    private async void btnAutoUpdate_Click(object sender, RoutedEventArgs e)
    {
        Error = "";
        if (_release != null)
        {
            if (!Downloading)
            {
                SetDownloadingStatus(status: true);
                ReleaseAsset asset = _release.Assets.FirstOrDefault(predicate: a => a.Name.EndsWith(value: ".msi"));
                downloadFile = Path.Combine(path1: _tmpFolder, path2: asset.Name);

                MaterialDesignThemes.Wpf.ButtonProgressAssist.SetIsIndicatorVisible(element: btnAutoUpdate, isIndicatorVisible: true);
                try
                {
                    await WebClient.DownloadFileTaskAsync(address: new Uri(uriString: asset.BrowserDownloadUrl), fileName: downloadFile);
                }
                catch (WebException ex)
                {
                    if (ex.Status != WebExceptionStatus.RequestCanceled)
                    {
                        Error = ex.Message;
                    }
                }

                SetDownloadingStatus(status: false);
                MaterialDesignThemes.Wpf.ButtonProgressAssist.SetIsIndicatorVisible(element: btnAutoUpdate, isIndicatorVisible: false);

                if (File.Exists(path: downloadFile))
                {
                    ShellLauncher.Open(target: downloadFile);
                    System.Windows.Application.Current.Shutdown();
                }
            }
            else
            {
                SetDownloadingStatus(status: false);
                WebClient.CancelAsync();
            }
        }
    }

    private void btnManualUpdate_Click(object sender, RoutedEventArgs e)
    {
        ShellLauncher.Open(target: _release.HtmlUrl);
    }

    private void SetDownloadingStatus(bool status)
    {
        Downloading = status;
        DownloadingChanged(sender: this, e: EventArgs.Empty);
        if (status)
        {
            btnAutoUpdateIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.CancelCircleOutline;
            btnAutoUpdateText.Text = "Zrušit";
        }
        else
        {
            btnAutoUpdateIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.DownloadCircleOutline;
            btnAutoUpdateText.Text = "Nainstalovat";
        }
    }
}