using MaterialDesignThemes.Wpf;
using PhotoApp.Dialogs;
using WirePix.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Octokit;
using WirePix.Devices.Contracts;
using WirePix.Devices.Windows;
using WirePix.Core.Devices;
using WirePix.Core.Import;
using WirePix.Core.Import.Contracts;
using WirePix.Core.Naming.Templates;
using WirePix.Core.Storage;
using WirePix.Core.Validation;
using Application = System.Windows.Application;
using DateRange = WirePix.Core.Models.Settings.DateRange;

namespace PhotoApp;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly string logFolder = Application.Current.Resources[key: Properties.Keys.LogsFolder].ToString();

    public DownloadSettings DownloadSettings { get; set; }
    public List<string> Profiles { get; set; }

    public DeviceCatalog DeviceCatalog { get; }
    public DeviceViewModel SelectedDevice { get; private set; }
    private readonly IMediaDeviceProvider deviceProvider;
    private readonly ProfileStore profileStore;
    public ProgressDialog progressDialog { get; private set; } = null;
    private ImportSession importSession;
    private CancellationTokenSource operationCancellation;


    //dialog error
    public static int RESULT_ERROR = -1;
    public static int RESULT_CANCEL = 0;
    public static int RESULT_OK = 1;

    private static int REQUEST_PROFILE_DELETE = 10;

    private Border normalBorder = new();
    private Border errorBorder = new();
    public Style mainTextStyle { get; private set; }

    public event PropertyChangedEventHandler PropertyChanged;

    public MainWindow()
    {
        DataContext = this;
        DownloadSettings = new DownloadSettings();
        Profiles = new List<string>();
        deviceProvider = new WindowsMediaDeviceProvider();
        profileStore = new ProfileStore(App.DataPaths.Profiles);
        DeviceCatalog = new DeviceCatalog(
            deviceProvider,
            new DeviceHistoryStore(Path.Combine(App.DataPaths.Data, "Devices.xml")));
        DeviceCatalog.Load();

        InitializeComponent();

        if (Properties.Settings.Default.CheckUpdateOnStartup)
        {
            CheckNewVersion();
        }

        progressDialog = new ProgressDialog(window: this);

        spDeviceInfo.Visibility = Visibility.Hidden;

        ListConnectedDevices();

        //udalost pripojeni/odpojeni zarizeni -> aktualizace seznamu
        deviceProvider.DevicesChanged += DeviceProvider_DevicesChanged;

        Properties.Settings.Default.PropertyChanged += Settings_Changed;

        normalBorder.BorderBrush = Brushes.Transparent;
        normalBorder.BorderThickness = new Thickness(uniformLength: 0);

        errorBorder.BorderBrush = Brushes.Red;
        errorBorder.BorderThickness = new Thickness(uniformLength: 3);

        mainTextStyle = (Style)FindResource(resourceKey: "MaterialDesignBody2TextBlock");

        GetProfiles();
    }

    private void Settings_Changed(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Properties.Settings.Default.TagLanguage))
        {
            icFolderTags.Items.Refresh();
            icFileTags.Items.Refresh();
        }
    }

    private void dhDialog_Loaded(object sender, RoutedEventArgs e)
    {
        Version currentVersion = Version.Parse(input: App.Instance.Version);
        Version lastRunVerison = Version.Parse(input: Properties.Settings.Default.LastVersion);
        if (currentVersion > lastRunVerison)
        {
            ShowChangelog();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(sender: this, e: new PropertyChangedEventArgs(propertyName: propertyName));
    }

    private async void ListBoxDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedDevice != null && ListBoxDevices.SelectedItem is DeviceInfo selectedInfo && selectedInfo.Id != SelectedDevice.Id)
        {
            operationCancellation?.Cancel();
        }

        if (ListBoxDevices.SelectedItem != null)
        {
            tbSelectDeviceError.Visibility = Visibility.Collapsed;
            spDeviceInfo.Visibility = Visibility.Visible;
            ListBoxDevices.BorderBrush = Brushes.Black;
            ListBoxDevices.BorderThickness = new Thickness(uniformLength: 1);

            //zmena vybraneho zarizeni
            DeviceCatalog.SelectDevice(index: ListBoxDevices.SelectedIndex);
            await UpdateSelectedDeviceAsync();
        }
        else
        {
            SelectedDevice = null;
            importSession = null;
            OnPropertyChanged(nameof(SelectedDevice));
            spDeviceInfo.Visibility = Visibility.Collapsed;
            ListBoxDevices.IsEnabled = true;
        }
    }


    //nalezeni a vypis vsech zarizeni vyuzivajicich MTP
    private async void ListConnectedDevices()
    {
        try
        {
            int selectedIndex = await DeviceCatalog.RefreshAsync();
            ListBoxDevices.SelectedIndex = selectedIndex;
            if (selectedIndex >= 0 && !ReferenceEquals(SelectedDevice?.Device, DeviceCatalog.SelectedDevice))
            {
                await UpdateSelectedDeviceAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // The window is closing or a refresh was superseded.
        }
        catch (ObjectDisposedException)
        {
            // A queued device notification can complete while the window is closing.
        }
    }

    private void DeviceProvider_DevicesChanged(object sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(callback: ListConnectedDevices);
    }

    private void GetProfiles(string SelectProfile = "")
    {
        string selectedProfile = SelectProfile;
        if (selectedProfile.Length == 0 && cbProfiles.SelectedItem != null)
        {
            selectedProfile = cbProfiles.SelectedItem.ToString();
        }

        Profiles = profileStore.GetProfileNames().ToList();

        OnPropertyChanged(propertyName: "Profiles");

        cbProfiles.SelectedIndex = Profiles.IndexOf(item: selectedProfile);

        if (Profiles.Count > 0 && selectedProfile.Length > 0)
        {
            btnDeleteProfile.IsEnabled = true;
        }
        else
        {
            btnDeleteProfile.IsEnabled = false;
        }
    }

    //obnovit seznam pripojenych zarizeni
    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        ListConnectedDevices();
    }

    private async void FindFiles_Click(object sender, RoutedEventArgs e)
    {
        if (CheckDeviceSelected())
        {
            await RunImportAsync(execute: false);
        }
    }

    private async void CopyFiles_Click(object sender, RoutedEventArgs e)
    {
        if (CheckSettings())
        {
            await RunImportAsync(execute: true);
        }
    }

    private void RemoveAllErrors()
    {
        ListBoxDevices.BorderBrush = Brushes.Black;
        ListBoxDevices.BorderThickness = new Thickness(uniformLength: 1);
        IEnumerable<TextBlock> errorBlocks = FindVisualChildren<TextBlock>(depObj: this).Where(predicate: x => x.Name.StartsWith(value: "tb") && x.Name.EndsWith(value: "Error"));
        foreach (TextBlock tb in errorBlocks)
        {
            tb.Visibility = Visibility.Collapsed;
        }

        IEnumerable<Button> errorButtons = FindVisualChildren<Button>(depObj: this).Where(predicate: x => x.BorderBrush == errorBorder.BorderBrush);
        foreach (Button btn in errorButtons)
        {
            btn.BorderBrush = normalBorder.BorderBrush;
            btn.BorderThickness = normalBorder.BorderThickness;
        }
    }

    public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
    {
        if (depObj != null)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(reference: depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(reference: depObj, childIndex: i);
                if (child != null && child is T)
                {
                    yield return (T)child;
                }

                foreach (T childOfChild in FindVisualChildren<T>(depObj: child))
                {
                    yield return childOfChild;
                }
            }
        }
    }

    private bool CheckSettings()
    {
        RemoveAllErrors();
        IReadOnlyList<ImportValidationIssue> issues = ImportSettingsValidator.Validate(
            DeviceCatalog.SelectedDevice,
            DeviceCatalog.SelectedDeviceInfo,
            DownloadSettings);
        foreach (ImportValidationIssue issue in issues)
        {
            ShowValidationIssue(issue.Code);
        }
        return issues.Count == 0;
    }

    private async Task UpdateSelectedDeviceAsync()
    {
        if (DeviceCatalog.SelectedDevice == null)
        {
            SelectedDevice = null;
            importSession = null;
            OnPropertyChanged(nameof(SelectedDevice));
            return;
        }

        SelectedDevice = new DeviceViewModel(DeviceCatalog.SelectedDevice);
        importSession = new ImportSession(new ImportCoordinator(App.DataPaths.Temp, App.DataPaths.Logs));
        OnPropertyChanged(nameof(SelectedDevice));
        try
        {
            await SelectedDevice.LoadAsync();
        }
        catch
        {
            // Device details are optional and can become unavailable after selection.
        }
    }

    private bool CheckDeviceSelected()
    {
        var correct = true;
        if (DeviceCatalog.SelectedDevice == null)
        {
            ListBoxDevices.BorderBrush = errorBorder.BorderBrush;
            ListBoxDevices.BorderThickness = errorBorder.BorderThickness;
            SetErrorMessage(tb: tbSelectDeviceError, message: Properties.Resources.SelectDevice);
            correct = false;
        }

        return correct;
    }

    private void ShowValidationIssue(ImportValidationCode code)
    {
        switch (code)
        {
            case ImportValidationCode.DeviceNotSelected:
                CheckDeviceSelected();
                break;
            case ImportValidationCode.DeviceNameMissing:
                SetErrorMessage(tbDeviceNameError, Properties.Resources.DeviceNameEmpty);
                break;
            case ImportValidationCode.RootMissing:
                BtnError(btnMainFolder); SetErrorMessage(tbRootError, Properties.Resources.NoDownloadFolder);
                break;
            case ImportValidationCode.RootNotFound:
                BtnError(btnMainFolder); SetErrorMessage(tbRootError, Properties.Resources.FolderDoesNotExist);
                break;
            case ImportValidationCode.BackupMissing:
                BtnError(btnChooseBackupDest); SetErrorMessage(tbBackupError, Properties.Resources.NoBackupFolder);
                break;
            case ImportValidationCode.BackupNotFound:
                BtnError(btnChooseBackupDest); SetErrorMessage(tbBackupError, Properties.Resources.FolderDoesNotExist);
                break;
            case ImportValidationCode.FolderTemplateMissing:
                BtnError(btnFolderStruct); SetErrorMessage(tbFolderStructError, Properties.Resources.NoFolderStructure);
                break;
            case ImportValidationCode.FileTemplateMissing:
                BtnError(btnFileStruct); SetErrorMessage(tbFileStructError, Properties.Resources.NoFileStructure);
                break;
            case ImportValidationCode.ThumbnailDirectoryMissing:
                BtnError(btnChooseThumbDest); SetErrorMessage(tbThumbnailDestError, Properties.Resources.NoThumbnailFolder);
                break;
            case ImportValidationCode.ThumbnailDirectoryNotFound:
                BtnError(btnChooseThumbDest); SetErrorMessage(tbThumbnailDestError, Properties.Resources.FolderDoesNotExist);
                break;
            case ImportValidationCode.ThumbnailSizeInvalid:
                SetErrorMessage(tbThumbnailError, Properties.Resources.CannotBeZero);
                break;
            case ImportValidationCode.DateRangeInvalid:
                SetErrorMessage(tbDateRangeError, Properties.Resources.DateRange_StartGreater);
                break;
        }
    }

    private void SetErrorMessage(TextBlock tb, string message)
    {
        tb.Text = message;
        tb.Visibility = Visibility.Visible;
    }

    private async Task RunImportAsync(bool execute)
    {
        if ((bool)cbNewFiles.IsChecked)
            DownloadSettings.Date.Start = DeviceCatalog.SelectedDeviceInfo.LastBackup;

        operationCancellation?.Dispose();
        operationCancellation = new CancellationTokenSource();
        DateTime startedAt = DateTime.Now;
        SelectedDevice.FileSearchStatus = FileSearchState.Searching;
        progressDialog.btnCancel.Content = Properties.Resources.Cancel;
        _ = DialogHost.Show(progressDialog, "RootDialog");
        var progress = new Progress<ImportProgress>(ReportImportProgress);
        try
        {
            if (!execute)
            {
                ImportPlan plan = await importSession.CreatePlanAsync(
                    SelectedDevice.Device, DownloadSettings, progress, operationCancellation.Token);
                SelectedDevice.FileSearchStatus = plan.Files.Count > 0 ? FileSearchState.Ready : FileSearchState.Unknown;
                lblResult.Text = $"{Properties.Resources.FilesFoundTotal}: {plan.Files.Count}\n" +
                                 $"{Properties.Resources.FilesToDownload}: {plan.Files.Count}";
                btnShowLog.Visibility = Visibility.Collapsed;
            }
            else
            {
                ImportResult result = await importSession.ExecuteAsync(
                    SelectedDevice.Device,
                    DownloadSettings,
                    new NamingContext
                    {
                        DeviceName = DeviceCatalog.SelectedDeviceInfo.Name,
                        TagLanguage = Properties.Settings.Default.TagLanguage,
                        UseTagLanguage = Properties.Settings.Default.UseDifferentLangForTags
                    },
                    progress,
                    operationCancellation.Token);
                SelectedDevice.FileSearchStatus = result.FilesTotal > 0 ? FileSearchState.Ready : FileSearchState.Unknown;
                lblResult.Text = $"{Properties.Resources.FilesFoundTotal}: {result.FilesTotal}\n" +
                                 $"{Properties.Resources.FilesDownloadedTotal}: {result.FilesDone}/{result.FilesTotal}\n" +
                                 $"{Properties.Resources.FilesDownloadErrorTotal}: {result.Errors}";
                DeviceCatalog.RecordSuccessfulImport(startedAt, DownloadSettings, result);
                btnShowLog.Visibility = result.Errors > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        catch (OperationCanceledException)
        {
            SelectedDevice.FileSearchStatus = FileSearchState.Unknown;
            lblResult.Text = Properties.Resources.ResultCanceled;
        }
        catch
        {
            SelectedDevice.FileSearchStatus = FileSearchState.Unknown;
            ErrorDialog(Properties.Resources.FileCheckError);
            ListConnectedDevices();
        }
        finally
        {
            dhDialog.IsOpen = false;
        }
    }

    private void ReportImportProgress(ImportProgress progress)
    {
        string task = progress.Stage switch
        {
            ImportStage.Searching => Properties.Resources.FileSearch,
            ImportStage.Filtering => Properties.Resources.DeviceFileFilterDate,
            ImportStage.Downloading => Properties.Resources.DeviceCopyingFiles,
            ImportStage.GeneratingThumbnail => string.Format(Properties.Resources.DeviceGeneratingThumbnail, progress.CurrentFile ?? string.Empty),
            ImportStage.Deleting => Properties.Resources.DeviceDeletingFilesTask,
            _ => Properties.Resources.DeviceSortingFile
        };
        progressDialog.SetCurrentTask(task);
        progressDialog.SetCurrentDir(progress.CurrentFile ?? string.Empty);
        if (progress.Total == 0)
        {
            progressDialog.SetIndeterminateProgress();
            progressDialog.SetProgressMessage(string.Empty);
        }
        else
        {
            string message = string.Format(Properties.Resources.DeviceFilesDoneCount, progress.Completed, progress.Total);
            progressDialog.SetCurrentProgress(message, progress.Completed * 100 / progress.Total);
        }
    }

    public void worker_Cancel() => operationCancellation?.Cancel();


    //dialog struktura slozky
    private async void btnFolderStruct_Click(object sender, RoutedEventArgs e)
    {
        BtnNormal(button: btnFolderStruct);
        tbFolderStructError.Visibility = Visibility.Collapsed;
        var buttonGroups = new List<ButtonGroupStruct>
        {
            new(
                groupName: Properties.Resources.TagGroup_Date,
                btns: new List<string>
                {
                    Properties.TagCodes.YearLong,
                    Properties.TagCodes.Month,
                    Properties.TagCodes.Day
                },
                gridPos: new Point(x: 0, y: 0)),

            new(
                groupName: Properties.Resources.TagGroup_Custom,
                btns: new List<string>
                {
                    Properties.TagCodes.CustomText
                },
                gridPos: new Point(x: 1, y: 0)),

            new(
                groupName: Properties.Resources.TagGroup_Device,
                btns: new List<string>
                {
                    Properties.TagCodes.DeviceName,
                    Properties.TagCodes.DeviceManuf
                },
                gridPos: new Point(x: 0, y: 1)),

            new(
                groupName: Properties.Resources.TagGroup_Folder,
                btns: new List<string>
                {
                    Properties.TagCodes.NewFolder
                },
                gridPos: new Point(x: 0, y: 2)),

            new(
                groupName: Properties.Resources.TagGroup_Separator,
                btns: new List<string>
                {
                    Properties.TagCodes.Hyphen,
                    Properties.TagCodes.Underscore
                },
                gridPos: new Point(x: 1, y: 1))
        };

        var nameDialog = new FolderStructDialog(window: this, buttons: buttonGroups, initStructure: DownloadSettings.Paths.FolderTags);
        await DialogHost.Show(content: nameDialog, dialogIdentifier: "RootDialog");
    }

    //zobrazeni chybove zpravy
    public void ErrorDialog(string message)
    {
        var errorDialog = new ErrorDialog(window: this, msg: message);
        DialogHost.Show(content: errorDialog, dialogIdentifier: "RootDialog");
    }

    //zavreni dialogu a ziskani vracenych hodnot
    public void DialogClose(object sender, object result = null, int resultCode = -1, int requestCode = -1)
    {
        if (resultCode == RESULT_OK)
        {
            if (sender.GetType() == typeof(FolderStructDialog))
            {
                DownloadSettings.Paths.FolderTags = (List<List<string>>)result;
            }
            else if (sender.GetType() == typeof(FileStructDialog))
            {
                DownloadSettings.Paths.FileTags = (List<string>)result;
            }
            else if (sender.GetType() == typeof(SaveDialog))
            {
                var options = (SaveOptions)result;
                DownloadSettings.Save(options: options);
                GetProfiles(SelectProfile: options.FileName); // načte nový profil
            }
            else if (sender.GetType() == typeof(YesNoDialog))
            {
                if ((int)result == YesNoDialog.RESULT_YES && requestCode == REQUEST_PROFILE_DELETE)
                {
                    DownloadSettings.Delete(profileName: cbProfiles.SelectedItem.ToString());
                    GetProfiles();
                    OnPropertyChanged(propertyName: "Profiles");
                }
            }
        }

        DialogHost.CloseDialogCommand.Execute(parameter: null, target: null);
    }

    //vytvoreni a zobrazeni dialogu pro nazev souboru
    private async void btnFileStruct_Click(object sender, RoutedEventArgs e)
    {
        BtnNormal(button: btnFileStruct);
        tbFileStructError.Visibility = Visibility.Collapsed;
        var buttonGroups = new List<ButtonGroupStruct>
        {
            new(
                groupName: Properties.Resources.TagGroup_Date,
                btns: new List<string>
                {
                    Properties.TagCodes.YearLong,
                    Properties.TagCodes.Month,
                    Properties.TagCodes.Day
                },
                gridPos: new Point(x: 0, y: 0)),

            new(
                groupName: Properties.Resources.TagGroup_Other,
                btns: new List<string>
                {
                    Properties.TagCodes.CustomText,
                    Properties.TagCodes.FileName
                },
                gridPos: new Point(x: 1, y: 0)),

            new(
                groupName: Properties.Resources.TagGroup_Device,
                btns: new List<string>
                {
                    Properties.TagCodes.DeviceName,
                    Properties.TagCodes.DeviceManuf
                },
                gridPos: new Point(x: 0, y: 1)),

            // new ButtonGroupStruct(
            //     "Číslování",
            //     new List<string>(){ 
            //         Properties.TagCodes.SequenceNum},
            //     new Point(0,2)),

            new(
                groupName: Properties.Resources.TagGroup_Separator,
                btns: new List<string>
                {
                    Properties.TagCodes.Hyphen,
                    Properties.TagCodes.Underscore
                },
                gridPos: new Point(x: 1, y: 1))
        };

        var nameDialog = new FileStructDialog(window: this, buttons: buttonGroups, initStructure: DownloadSettings.Paths.FileTags);
        await DialogHost.Show(content: nameDialog, dialogIdentifier: "RootDialog");
    }

    // vyber slozek
    private void btnMainFolder_Click(object sender, RoutedEventArgs e)
    {
        BtnNormal(button: btnMainFolder);
        tbRootError.Visibility = Visibility.Collapsed;
        string result = ChooseDirectory(initDir: DownloadSettings.Paths.Root);
        if (result != string.Empty)
        {
            DownloadSettings.Paths.Root = result;
        }
    }

    private void btnChooseBackupDest_Click(object sender, RoutedEventArgs e)
    {
        BtnNormal(button: btnChooseBackupDest);
        tbBackupError.Visibility = Visibility.Collapsed;
        string result = ChooseDirectory(initDir: DownloadSettings.Paths.Backup);
        if (result != string.Empty)
        {
            DownloadSettings.Paths.Backup = result;
        }
    }

    private void btnChooseThumbDest_Click(object sender, RoutedEventArgs e)
    {
        BtnNormal(button: btnChooseThumbDest);
        tbThumbnailDestError.Visibility = Visibility.Collapsed;
        string result = ChooseDirectory(initDir: DownloadSettings.Paths.Thumbnail);
        if (result != string.Empty)
        {
            DownloadSettings.Paths.Thumbnail = result;
        }
    }

    private string ChooseDirectory(string initDir)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        dialog.SelectedPath = DownloadSettings.Paths.Thumbnail;
        System.Windows.Forms.DialogResult result = dialog.ShowDialog();
        if (result.ToString() != string.Empty)
        {
            return dialog.SelectedPath;
        }
        else
        {
            return string.Empty;
        }
    }


    // zmena stavu tlacitek (error, normal)
    private void BtnError(Button button)
    {
        button.BorderBrush = errorBorder.BorderBrush;
        button.BorderThickness = errorBorder.BorderThickness;
    }

    private void BtnNormal(Button button)
    {
        button.BorderBrush = normalBorder.BorderBrush;
        button.BorderThickness = normalBorder.BorderThickness;
    }

    private void dpDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        var dp = sender as DatePicker;
        if (dp.SelectedDate == null)
        {
            dp.SelectedDate = DateTime.Now;
        }

        if (DownloadSettings.Date.Start > DownloadSettings.Date.End)
        {
            SetErrorMessage(tb: tbDateRangeError, message: Properties.Resources.DateRange_StartGreater);
        }
        else if (tbDateRangeError != null)
        {
            tbDateRangeError.Visibility = Visibility.Collapsed;
        }
    }

    private void tbValue_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        var regex = new Regex(pattern: @"^[0-9]+");
        e.Handled = !regex.IsMatch(input: e.Text);
    }


    // pokud by pole melo byt prazdne -> chyba data bindingu (potreba hondota int)
    private void tbValue_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox.Text.Length == 1 && (e.Key == System.Windows.Input.Key.Back || e.Key == System.Windows.Input.Key.Delete))
        {
            textBox.Text = "0";
            textBox.SelectionStart = 1;
            textBox.SelectionLength = 0;
            e.Handled = true;
        }
    }

    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
        if (cbProfiles.SelectedIndex == -1)
        {
            btnSaveAs_Click(sender: sender, e: e);
        }
        else
        {
            DownloadSettings.Save();
        }
    }

    private async void btnSaveAs_Click(object sender, RoutedEventArgs e)
    {
        var saveDialog = new SaveDialog(window: this, options: DownloadSettings.SaveOptions);
        await DialogHost.Show(content: saveDialog, dialogIdentifier: "RootDialog");
    }

    private void btnLoad_Click(object sender, RoutedEventArgs e)
    {
        GetProfiles();
    }

    private void btnDeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        ShowYesNoDialog(message: string.Format(format: Properties.Resources.DeleteProfilePrompt, arg0: cbProfiles.SelectedItem), request_code: REQUEST_PROFILE_DELETE);
    }

    private void cbProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var cb = sender as ComboBox;
        if (cb.SelectedItem != null)
        {
            if (!DownloadSettings.Load(profileName: cb.SelectedItem.ToString()))
            {
                ShowYesNoDialog(message: string.Format(format: Properties.Resources.Profile_Load_Error, arg0: cb.SelectedItem), request_code: REQUEST_PROFILE_DELETE);
            }

            OnPropertyChanged(propertyName: "DownloadSettings");

            RemoveAllErrors();
            CheckSettings();

            btnDeleteProfile.IsEnabled = true;
        }
        else
        {
            btnDeleteProfile.IsEnabled = false;
        }
    }

    public static StackPanel CreateIconPanel(string text, PackIconKind iconKind, int level, Style textStyle)
    {
        var sp = new StackPanel();
        sp.Orientation = Orientation.Horizontal;
        sp.Margin = new Thickness(left: level * 10, top: 0, right: 0, bottom: 0);

        var icon = new PackIcon();
        icon.Kind = iconKind;

        sp.Children.Add(element: icon);

        var textBlock = new TextBlock();
        textBlock.Style = textStyle;
        textBlock.FontWeight = FontWeights.SemiBold;
        textBlock.Margin = new Thickness(left: 5, top: 0, right: 0, bottom: 0);
        textBlock.Text = text;
        sp.Children.Add(element: textBlock);

        return sp;
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        operationCancellation?.Cancel();
        operationCancellation?.Dispose();
        deviceProvider.DevicesChanged -= DeviceProvider_DevicesChanged;
        if (deviceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }

        DeviceCatalog.Save();
        ApplicationDataMaintenance.ClearTemporaryFiles(App.DataPaths);
    }

    private void cbNewFiles_Checked(object sender, RoutedEventArgs e)
    {
        DownloadSettings.Date = new DateRange();
        if (DeviceCatalog.SelectedDevice != null)
        {
            DownloadSettings.Date.Start = DeviceCatalog.SelectedDeviceInfo.LastBackup;
        }
    }

    private void tbValue_LostFocus(object sender, RoutedEventArgs e)
    {
        if (DownloadSettings.ThumbnailSettings.Value == 0)
        {
            SetErrorMessage(tb: tbThumbnailError, message: Properties.Resources.CannotBeZero);
        }
    }

    private void tbValue_GotFocus(object sender, RoutedEventArgs e)
    {
        tbThumbnailError.Visibility = Visibility.Collapsed;
    }

    private void cbDateRange_Checked(object sender, RoutedEventArgs e)
    {
        DownloadSettings.Date.Start = DownloadSettings.Date.Start.Date;
        if (DownloadSettings.Date.Start.Date == new DateTime().Date)
        {
            DownloadSettings.Date = new DateRange();
            OnPropertyChanged(propertyName: "DownloadSettings");
        }
    }

    private void btnShowLog_Click(object sender, RoutedEventArgs e)
    {
        var directory = new DirectoryInfo(path: logFolder);
        FileInfo lastLog = directory.GetFiles().OrderByDescending(keySelector: f => f.LastWriteTime).First();
        Process.Start(fileName: "explorer.exe", arguments: lastLog.FullName);
    }

    private void lblDeviceName_TextChanged(object sender, TextChangedEventArgs e)
    {
        var tb = (TextBox)sender;
        if (DeviceCatalog.SelectedDeviceIndex != -1 && tb.Text.Length == 0)
        {
            SetErrorMessage(tb: tbDeviceNameError, message: Properties.Resources.DeviceNameEmpty);
            ListBoxDevices.IsEnabled = false;
        }
        else
        {
            tbDeviceNameError.Visibility = Visibility.Collapsed;
            ListBoxDevices.IsEnabled = true;
        }
    }

    private async void btnInfoClick(object sender, RoutedEventArgs e)
    {
        var AppInfoDialog = new AppInfoDialog(window: this);
        await DialogHost.Show(content: (object)AppInfoDialog, dialogIdentifier: "RootDialog");
    }

    private async void btnSettings_Click(object sender, RoutedEventArgs e)
    {
        var AppSettingsDialog = new AppSettingsDialog(window: this);
        await DialogHost.Show(content: AppSettingsDialog, dialogIdentifier: "RootDialog");
    }

    private async void btnFeedback_Click(object sender, RoutedEventArgs e)
    {
        var AppFeedbackDialog = new AppFeedbackDialog(window: this);
        await DialogHost.Show(content: AppFeedbackDialog, dialogIdentifier: "RootDialog");
    }

    private async void ShowUpdateDialog(Octokit.Release release)
    {
        var UpdateDialog = new UpdateDialog(window: this, release: release);
        await DialogHost.Show(content: UpdateDialog, dialogIdentifier: "RootDialog");
    }

    private async void ShowYesNoDialog(string message, int request_code)
    {
        var ynDialog = new YesNoDialog(window: this, text: message, requestCode: request_code);
        await DialogHost.Show(content: ynDialog, dialogIdentifier: "RootDialog");
    }

    public async void ShowLibraries(object sender = null)
    {
        DialogHost.CloseDialogCommand.Execute(parameter: null, target: null);

        var LibrariesDialog = new UsedLibraries();
        await DialogHost.Show(content: LibrariesDialog, dialogIdentifier: "RootDialog");

        if (sender != null)
        {
            await DialogHost.Show(content: sender, dialogIdentifier: "RootDialog");
        }
    }

    public async void ShowChangelog(object sender = null)
    {
        DialogHost.CloseDialogCommand.Execute(parameter: null, target: null);
        var ChangelogDialog = new ChangelogDialog(parentHeight: ActualHeight, parentWidth: ActualWidth);
        await DialogHost.Show(content: ChangelogDialog, dialogIdentifier: "RootDialog");

        if (sender != null)
        {
            await DialogHost.Show(content: sender, dialogIdentifier: "RootDialog");
        }
    }

    private async void CheckNewVersion()
    {
        Version currentVersion = Version.Parse(input: App.Instance.Version);
        try
        {
            var github = new Octokit.GitHubClient(productInformation: new Octokit.ProductHeaderValue(name: "WirePix"));
            Release release = await github.Repository.Release.GetLatest(owner: "KurekMartin", name: "WirePix");
            Version latestVersion = Version.Parse(input: release.TagName.Replace(oldValue: "v", newValue: ""));

            if (latestVersion > currentVersion)
            {
                SnackBar.MessageQueue.Enqueue(content: Properties.Resources.Update_NewVersionAvailable, actionContent: Properties.Resources.Show.ToUpper(), actionHandler: () => ShowUpdateDialog(release: release));
            }
        }
        catch (Exception)
        {
        }
    }
}
