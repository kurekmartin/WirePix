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
    private static readonly string _tmpFolder = System.Windows.Application.Current.Resources[Properties.Keys.TempFolder].ToString();
    private string downloadFile;
    private WebClient WebClient = new WebClient();

    public event PropertyChangedEventHandler PropertyChanged;
    public event EventHandler DownloadingChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            var file = new FileInfo(downloadFile);
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
                SetDownloadingStatus(true);
                var asset = _release.Assets.FirstOrDefault(a => a.Name.EndsWith(".msi"));
                downloadFile = Path.Combine(_tmpFolder, asset.Name);

                MaterialDesignThemes.Wpf.ButtonProgressAssist.SetIsIndicatorVisible(btnAutoUpdate, true);
                try
                {
                    await WebClient.DownloadFileTaskAsync(new Uri(asset.BrowserDownloadUrl), downloadFile);
                }
                catch (WebException ex)
                {
                    if (ex.Status != WebExceptionStatus.RequestCanceled)
                    {
                        Error = ex.Message;
                    }
                }

                SetDownloadingStatus(false);
                MaterialDesignThemes.Wpf.ButtonProgressAssist.SetIsIndicatorVisible(btnAutoUpdate, false);

                if (File.Exists(downloadFile))
                {
                    ShellLauncher.Open(downloadFile);
                    System.Windows.Application.Current.Shutdown();
                }
            }
            else
            {
                SetDownloadingStatus(false);
                WebClient.CancelAsync();
            }
        }
    }
    private void btnManualUpdate_Click(object sender, RoutedEventArgs e)
    {
        ShellLauncher.Open(_release.HtmlUrl);
    }

    private void SetDownloadingStatus(bool status)
    {
        Downloading = status;
        DownloadingChanged(this, EventArgs.Empty);
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