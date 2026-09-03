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
using WirePix.Core.Storage;

namespace PhotoApp;

/// <summary>
/// Interakční logika pro App.xaml
/// </summary>
public partial class App : Application
{
    private static List<Tuple<string, string>> _availableLanguages = new();

    private static Mutex _mutex = null;
    public static App Instance => (App)Current;
    public static ApplicationDataPaths DataPaths { get; private set; }

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
        DataPaths = ApplicationDataPaths.CreateDefault("WirePix");
        Resources.Add(key: Keys.MainFolder, value: DataPaths.Main);
        Resources.Add(key: Keys.TempFolder, value: DataPaths.Temp);
        Resources.Add(key: Keys.LogsFolder, value: DataPaths.Logs);
        Resources.Add(key: Keys.ProfilesFolder, value: DataPaths.Profiles);
        Resources.Add(key: Keys.DataFolder, value: DataPaths.Data);
        Resources.Add(key: Keys.CrashReportsFolder, value: DataPaths.CrashReports);
        DownloadSettings.ProfileDirectory = DataPaths.Profiles;
        ApplicationDataMaintenance.Prepare(DataPaths, Settings.Default.MaxLogs);

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
