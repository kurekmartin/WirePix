using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows;
using MaterialDesignThemes.Wpf;
using PhotoApp.Properties;

namespace PhotoApp;

/// <summary>
/// Interakční logika pro App.xaml
/// </summary>
public partial class App : Application
{
    private static List<Tuple<string, string>> _availableLanguages = new();

    private static Mutex _mutex = null;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string appName = "WirePix";
        bool createdNew;

        _mutex = new Mutex(initiallyOwned: true, name: appName, createdNew: out createdNew);

        if (!createdNew)
        {
            Current.Shutdown();
        }

        base.OnStartup(e: e);
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        if (Settings.Default.UpdateSettings)
        {
            Settings.Default.Upgrade();
            Settings.Default.UpdateSettings = false;
            Settings.Default.Save();
        }


        SetLanguage();
        string appData = Environment.GetFolderPath(folder: Environment.SpecialFolder.ApplicationData);
        var mainFolder = "WirePix";
        Resources.Add(key: Keys.MainFolder, value: Path.Combine(path1: appData, path2: mainFolder));
        Resources.Add(key: Keys.TempFolder, value: Path.Combine(path1: Path.GetTempPath(), path2: mainFolder));
        Resources.Add(key: Keys.LogsFolder, value: Path.Combine(path1: appData, path2: mainFolder, path3: "Logs"));
        Resources.Add(key: Keys.ProfilesFolder, value: Path.Combine(path1: appData, path2: mainFolder, path3: "Profiles"));
        Resources.Add(key: Keys.DataFolder, value: Path.Combine(path1: appData, path2: mainFolder, path3: "Data"));
        Resources.Add(key: Keys.CrashReportsFolder, value: Path.Combine(path1: appData, path2: mainFolder, path3: "Crash Reports"));
        DownloadSettings.ProfileDirectory = Resources[key: Keys.ProfilesFolder].ToString();

        IEnumerator keys = Current.Resources.Keys.GetEnumerator();
        while (keys.MoveNext())
        {
            var key = keys.Current.ToString();
            if (key.Contains(value: "Folder"))
            {
                Directory.CreateDirectory(path: Current.Resources[key: key].ToString());
            }
        }

        IEnumerable<string> files = Directory.GetFiles(path: Current.Resources[key: Keys.TempFolder].ToString()).Where(predicate: x => x.EndsWith(value: ".msi"));
        foreach (string file in files)
        {
            File.Delete(path: file);
        }

        var logFolder = Current.Resources[key: Keys.LogsFolder].ToString();
        int maxLogs = Settings.Default.MaxLogs;
        var logFiles = new DirectoryInfo(path: logFolder);
        IEnumerable<FileInfo> filesToDelete = logFiles.GetFiles().OrderByDescending(keySelector: f => f.CreationTime).Skip(count: maxLogs);
        foreach (FileInfo file in filesToDelete)
        {
            file.Delete();
        }

        SetThemeMode(darkMode: Settings.Default.DarkMode);
    }

    public string Version => Assembly.GetEntryAssembly()?.GetName().Version?.ToString(fieldCount: 3);

    public static void SetThemeMode(bool darkMode)
    {
        var paletteHelper = new PaletteHelper();
        //Retrieve the app's existing theme
        ITheme theme = paletteHelper.GetTheme();
        if (darkMode)
        {
            theme.SetBaseTheme(baseTheme: Theme.Dark);
        }
        else
        {
            theme.SetBaseTheme(baseTheme: Theme.Light);
        }

        paletteHelper.SetTheme(theme: theme);
        if (darkMode != Settings.Default.DarkMode)
        {
            Settings.Default.DarkMode = darkMode;
            Settings.Default.Save();
        }
    }

    public static void SetLanguage()
    {
        if (Settings.Default.Language != nameof(Languages.system))
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(name: Settings.Default.Language);
        }
        else
        {
            string language = CultureInfo.CurrentUICulture.Name.Split(separator: '-')[0];
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(name: language);
        }

        Console.WriteLine(format: "CurrentCulture is {0}.", arg0: CultureInfo.CurrentUICulture.Name);
    }

    public static List<Tuple<string, string>> GetAvailableLanguages()
    {
        if (_availableLanguages.Count() == 0)
        {
            ResourceSet res = Languages.ResourceManager.GetResourceSet(culture: CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false);
            foreach (DictionaryEntry language in res)
            {
                _availableLanguages.Add(item: new Tuple<string, string>(item1: language.Key.ToString(), item2: language.Value.ToString()));
            }
        }

        return _availableLanguages;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Settings.Default.LastVersion = Version;
        Settings.Default.Save();
        base.OnExit(e: e);
    }
}