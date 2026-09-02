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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Octokit;
using Usb.Events;
using Application = System.Windows.Application;
using DateRange = WirePix.Core.Models.Settings.DateRange;

namespace PhotoApp;

public enum TaskType
{
    FindFiles,
    CopyFiles,
    GetFileTypes
}

public struct WorkerResult
{
    public WorkerResult(int code, TaskType task)
    {
        this.code = code;
        this.task = task;
    }

    public int code;
    public TaskType task;
}

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly string logFolder = Application.Current.Resources[key: Properties.Keys.LogsFolder].ToString();
    private static readonly string tmpFolder = Application.Current.Resources[key: Properties.Keys.TempFolder].ToString();

    public DownloadSettings DownloadSettings { get; set; }
    public List<string> Profiles { get; set; }

    public DeviceList DeviceList { get; set; }
    private static readonly IUsbEventWatcher usbEventWatcher = new UsbEventWatcher();
    public ProgressDialog progressDialog { get; private set; } = null;
    private BackgroundWorker backgroundWorker = null;
    private DateTime backupStart = new();


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
        DeviceList = new DeviceList();

        DeviceList.Load();

        InitializeComponent();

        if (Properties.Settings.Default.CheckUpdateOnStartup)
        {
            CheckNewVersion();
        }

        progressDialog = new ProgressDialog(window: this);

        spDeviceInfo.Visibility = Visibility.Hidden;

        ListConnectedDevices();

        //udalost pripojeni/odpojeni zarizeni -> aktualizace seznamu
        usbEventWatcher.UsbDeviceAdded += (_, device) => Dispatcher.Invoke(callback: ListConnectedDevices);
        usbEventWatcher.UsbDeviceRemoved += (_, device) => Dispatcher.Invoke(callback: ListConnectedDevices);

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
        Version currentVersion = Version.Parse(input: ((App)Application.Current).Version);
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

    private void ListBoxDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListBoxDevices.SelectedItem != null)
        {
            tbSelectDeviceError.Visibility = Visibility.Collapsed;
            spDeviceInfo.Visibility = Visibility.Visible;
            ListBoxDevices.BorderBrush = Brushes.Black;
            ListBoxDevices.BorderThickness = new Thickness(uniformLength: 1);

            //zmena vybraneho zarizeni
            DeviceList.SelectDevice(index: ListBoxDevices.SelectedIndex);

            //DeviceList.SelectedDevice.FileTypes();
        }
        else
        {
            spDeviceInfo.Visibility = Visibility.Collapsed;
            ListBoxDevices.IsEnabled = true;
        }
    }


    //nalezeni a vypis vsech zarizeni vyuzivajicich MTP
    private void ListConnectedDevices()
    {
        ListBoxDevices.SelectedIndex = DeviceList.UpdateDevices();
    }

    private void GetProfiles(string SelectProfile = "")
    {
        var profilesPath = Application.Current.Resources[key: Properties.Keys.ProfilesFolder].ToString();
        string selectedProfile = SelectProfile;
        if (selectedProfile.Length == 0 && cbProfiles.SelectedItem != null)
        {
            selectedProfile = cbProfiles.SelectedItem.ToString();
        }

        Profiles = Directory.GetFiles(path: profilesPath, searchPattern: "*.xml").Select(selector: f => Path.GetFileNameWithoutExtension(path: f)).Where(predicate: x => DownloadSettings.IsValid(profileName: x)).ToList();

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

    //vyhledani vsech souboru v adresari (hleda i v podadresarich)
    private void FindFiles(object sender, DoWorkEventArgs e)
    {
        var worker = sender as BackgroundWorker;
        DeviceList.SelectedDevice.GetFilesByDate(worker: worker, e: e, settings: DownloadSettings);
    }

    private void CopyFiles(object sender, DoWorkEventArgs e)
    {
        var worker = sender as BackgroundWorker;
        DeviceList.SelectedDevice.CopyFiles(worker: worker, e: e, settings: DownloadSettings);
    }

    private void GetFileTypes(object sender, DoWorkEventArgs e)
    {
        var worker = sender as BackgroundWorker;
        DeviceList.SelectedDevice.FileTypes(worker: worker, e: e);
    }

    //obnovit seznam pripojenych zarizeni
    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        ListConnectedDevices();
    }

    private void FindFiles_Click(object sender, RoutedEventArgs e)
    {
        if (CheckDeviceSelected())
        {
            RunWorker(sender: sender, task: TaskType.FindFiles, showProgress: true);
        }
    }

    private void CopyFiles_Click(object sender, RoutedEventArgs e)
    {
        if (CheckSettings())
        {
            backupStart = DateTime.Now;
            RunWorker(sender: sender, task: TaskType.CopyFiles, showProgress: true);
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
        var correct = true;

        correct = CheckDeviceSelected();

        if (DeviceList.SelectedDeviceInfo.Name == null || DeviceList.SelectedDeviceInfo.Name.Length == 0)
        {
            SetErrorMessage(tb: tbDeviceNameError, message: Properties.Resources.DeviceNameEmpty);
            correct = false;
        }

        if (DownloadSettings.Paths.Root == null || DownloadSettings.Paths.Root.Length == 0)
        {
            BtnError(button: btnMainFolder);
            SetErrorMessage(tb: tbRootError, message: Properties.Resources.NoDownloadFolder);
            correct = false;
        }
        else if (!Directory.Exists(path: DownloadSettings.Paths.Root))
        {
            BtnError(button: btnMainFolder);
            SetErrorMessage(tb: tbRootError, message: Properties.Resources.FolderDoesNotExist);
            correct = false;
        }

        if (DownloadSettings.Backup)
        {
            if (DownloadSettings.Paths.Backup == null || DownloadSettings.Paths.Backup.Length == 0)
            {
                BtnError(button: btnChooseBackupDest);
                SetErrorMessage(tb: tbBackupError, message: Properties.Resources.NoBackupFolder);
                correct = false;
            }
            else if (!Directory.Exists(path: DownloadSettings.Paths.Backup))
            {
                BtnError(button: btnChooseBackupDest);
                SetErrorMessage(tb: tbBackupError, message: Properties.Resources.FolderDoesNotExist);
                correct = false;
            }
        }


        if (DownloadSettings.Paths.FolderTags == null || DownloadSettings.Paths.FolderTags.Count == 0)
        {
            BtnError(button: btnFolderStruct);
            SetErrorMessage(tb: tbFolderStructError, message: Properties.Resources.NoFolderStructure);
            correct = false;
        }

        if (DownloadSettings.Paths.FileTags == null || DownloadSettings.Paths.FileTags.Count == 0)
        {
            BtnError(button: btnFileStruct);
            SetErrorMessage(tb: tbFileStructError, message: Properties.Resources.NoFileStructure);
            correct = false;
        }

        if (DownloadSettings.Thumbnail)
        {
            if (DownloadSettings.Paths.Thumbnail == null || DownloadSettings.Paths.Thumbnail.Length == 0)
            {
                BtnError(button: btnChooseThumbDest);
                SetErrorMessage(tb: tbThumbnailDestError, message: Properties.Resources.NoThumbnailFolder);
                correct = false;
            }
            else if (!Directory.Exists(path: DownloadSettings.Paths.Thumbnail))
            {
                BtnError(button: btnChooseThumbDest);
                SetErrorMessage(tb: tbThumbnailDestError, message: Properties.Resources.FolderDoesNotExist);
                correct = false;
            }

            if (DownloadSettings.ThumbnailSettings.Value == 0)
            {
                SetErrorMessage(tb: tbThumbnailError, message: Properties.Resources.CannotBeZero);
                correct = false;
            }
        }

        if ((bool)cbDateRange.IsChecked && DownloadSettings.Date.Start > DownloadSettings.Date.End)
        {
            SetErrorMessage(tb: tbDateRangeError, message: Properties.Resources.DateRange_StartGreater);
            correct = false;
        }


        return correct;
    }

    private bool CheckDeviceSelected()
    {
        var correct = true;
        if (DeviceList.SelectedDevice == null)
        {
            ListBoxDevices.BorderBrush = errorBorder.BorderBrush;
            ListBoxDevices.BorderThickness = errorBorder.BorderThickness;
            SetErrorMessage(tb: tbSelectDeviceError, message: Properties.Resources.SelectDevice);
            correct = false;
        }

        return correct;
    }

    private void SetErrorMessage(TextBlock tb, string message)
    {
        tb.Text = message;
        tb.Visibility = Visibility.Visible;
    }

    //spusteni prace na pozadi podle typu ulohy
    private void RunWorker(object sender, TaskType task, bool showProgress)
    {
        backgroundWorker = new BackgroundWorker();
        backgroundWorker.WorkerSupportsCancellation = true;
        backgroundWorker.WorkerReportsProgress = showProgress;
        backgroundWorker.ProgressChanged += worker_ProgressChanged;
        backgroundWorker.RunWorkerCompleted += worker_RunWorkerCompleted;

        if (showProgress)
        {
            DialogHost.Show(content: progressDialog, dialogIdentifier: "RootDialog");
            progressDialog.btnCancel.Content = Properties.Resources.Cancel;
        }

        if ((bool)cbNewFiles.IsChecked)
        {
            DownloadSettings.Date.Start = DeviceList.SelectedDeviceInfo.LastBackup;
        }

        switch (task)
        {
            case TaskType.FindFiles:
                backgroundWorker.DoWork += FindFiles;
                break;
            case TaskType.CopyFiles:
                backgroundWorker.DoWork += CopyFiles;
                break;
            case TaskType.GetFileTypes:
                backgroundWorker.DoWork += GetFileTypes;
                break;
        }

        backgroundWorker.RunWorkerAsync();
    }


    //backgroundWorker update progress
    private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        var progress = (ProgressUpdateArgs)e.UserState;

        progressDialog.SetCurrentTask(taskName: progress.taskName);

        if (progress.indeterminateTask)
        {
            progressDialog.SetIndeterminateProgress();
            progressDialog.SetProgressMessage(message: progress.progressText);
        }
        else
        {
            progressDialog.SetCurrentProgress(progressMessage: progress.progressText, progress: e.ProgressPercentage, time: progress.timeRemain);
        }

        progressDialog.SetCurrentDir(dirName: progress.currentTask);
    }

    private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        dhDialog.IsOpen = false;
        if (e.Cancelled)
        {
            lblResult.Text = Properties.Resources.ResultCanceled;
            return;
        }

        var result = (WorkerResult)e.Result;
        if (result.code == RESULT_ERROR)
        {
            ErrorDialog(message: Properties.Resources.FileCheckError);
            ListConnectedDevices();
        }
        else if (result.code == RESULT_OK)
        {
            int total = DeviceList.SelectedDevice.FilesTotal;
            int downloaded = DeviceList.SelectedDevice.FilesDoneCount;
            int toDownload = DeviceList.SelectedDevice.FilesToCopyCount;
            int errors = DeviceList.SelectedDevice.Errors;
            lblResult.Text = $"{Properties.Resources.FilesFoundTotal}: {total}\n";
            if (result.task == TaskType.CopyFiles)
            {
                lblResult.Text += $"{Properties.Resources.FilesDownloadedTotal}: {downloaded}/{toDownload}\n" +
                                  $"{Properties.Resources.FilesDownloadErrorTotal}: {errors}";
                if (DownloadSettings.DownloadSelect == DownloadSelect.lastBackup && downloaded == toDownload)
                {
                    DeviceList.SelectedDeviceInfo.LastBackup = backupStart;
                }

                OnPropertyChanged(propertyName: "SelectedDeviceInfo");
                DeviceList.Save();
            }
            else if (result.task == TaskType.FindFiles)
            {
                lblResult.Text += $"{Properties.Resources.FilesToDownload}: {toDownload}";
            }

            if (errors > 0)
            {
                btnShowLog.Visibility = Visibility.Visible;
            }
            else
            {
                btnShowLog.Visibility = Visibility.Collapsed;
            }
        }
    }

    public void worker_Cancel()
    {
        backgroundWorker.CancelAsync();
    }


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
        DeviceList.Save();
        ClearTemp();
    }

    private void cbNewFiles_Checked(object sender, RoutedEventArgs e)
    {
        DownloadSettings.Date = new DateRange();
        if (DeviceList.SelectedDevice != null)
        {
            DownloadSettings.Date.Start = DeviceList.SelectedDeviceInfo.LastBackup;
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

    public static void ClearTemp()
    {
        Parallel.ForEach(source: Directory.EnumerateFiles(path: tmpFolder).Where(predicate: x => !x.EndsWith(value: ".msi")), body: (file) => { File.Delete(path: file); });
    }

    private void lblDeviceName_TextChanged(object sender, TextChangedEventArgs e)
    {
        var tb = (TextBox)sender;
        if (DeviceList.SelectedDeviceIndex != -1 && tb.Text.Length == 0)
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
        Version currentVersion = Version.Parse(input: ((App)Application.Current).Version);
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
        catch (Exception ex)
        {
        }
    }
}