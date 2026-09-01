using MaterialDesignThemes.Wpf;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Collections;
using System.Threading;

namespace PhotoApp
{
    /// <summary>
    /// Interakční logika pro App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static List<Tuple<string, string>> _availableLanguages = new List<Tuple<string, string>>();

        private static Mutex _mutex = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "WirePix";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                Current.Shutdown();
            }

            base.OnStartup(e);
        }
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (PhotoApp.Properties.Settings.Default.UpdateSettings)
            {
                PhotoApp.Properties.Settings.Default.Upgrade();
                PhotoApp.Properties.Settings.Default.UpdateSettings = false;
                PhotoApp.Properties.Settings.Default.Save();
            }


            SetLanguage();
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string mainFolder = "WirePix";
            Resources.Add(PhotoApp.Properties.Keys.MainFolder, Path.Combine(appData,mainFolder));
            Resources.Add(PhotoApp.Properties.Keys.TempFolder, Path.Combine(Path.GetTempPath(), mainFolder));
            Resources.Add(PhotoApp.Properties.Keys.LogsFolder, Path.Combine(appData, mainFolder, "Logs"));
            Resources.Add(PhotoApp.Properties.Keys.ProfilesFolder, Path.Combine(appData, mainFolder, "Profiles"));
            Resources.Add(PhotoApp.Properties.Keys.DataFolder, Path.Combine(appData, mainFolder, "Data"));
            Resources.Add(PhotoApp.Properties.Keys.CrashReportsFolder, Path.Combine(appData, mainFolder, "Crash Reports"));

            var keys = Current.Resources.Keys.GetEnumerator();
            while (keys.MoveNext())
            {
                string key = keys.Current.ToString();
                if (key.Contains("Folder"))
                {
                    Directory.CreateDirectory(Current.Resources[key].ToString());
                }
            }

            var files = Directory.GetFiles(Current.Resources[PhotoApp.Properties.Keys.TempFolder].ToString()).Where(x => x.EndsWith(".msi"));
            foreach (var file in files)
            {
                File.Delete(file);
            }

            var logFolder = Current.Resources[PhotoApp.Properties.Keys.LogsFolder].ToString();
            var maxLogs = PhotoApp.Properties.Settings.Default.MaxLogs;
            var logFiles = new DirectoryInfo(logFolder);
            var filesToDelete = logFiles.GetFiles().OrderByDescending(f => f.CreationTime).Skip(maxLogs);
            foreach (var file in filesToDelete)
            {
                file.Delete();
            }

            SetThemeMode(PhotoApp.Properties.Settings.Default.DarkMode);
        }

        public string Version
        {
            get
            {
                return Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3);
            }
        }

        public static void SetThemeMode(bool darkMode)
        {
            var paletteHelper = new PaletteHelper();
            //Retrieve the app's existing theme
            ITheme theme = paletteHelper.GetTheme();
            if (darkMode)
            {
                theme.SetBaseTheme(Theme.Dark);
            }
            else
            {
                theme.SetBaseTheme(Theme.Light);
            }
            paletteHelper.SetTheme(theme);
            if (darkMode != PhotoApp.Properties.Settings.Default.DarkMode)
            {
                PhotoApp.Properties.Settings.Default.DarkMode = darkMode;
                PhotoApp.Properties.Settings.Default.Save();
            }
        }

        public static void SetLanguage()
        {
            if (PhotoApp.Properties.Settings.Default.Language != nameof(PhotoApp.Properties.Languages.system))
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(PhotoApp.Properties.Settings.Default.Language);
            }
            else
            {
                string language = CultureInfo.CurrentUICulture.Name.Split('-')[0];
                System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);
            }
            Console.WriteLine("CurrentCulture is {0}.", CultureInfo.CurrentUICulture.Name);
        }

        public static List<Tuple<string, string>> GetAvailableLanguages()
        {
            if (_availableLanguages.Count() == 0)
            {
                var res = PhotoApp.Properties.Languages.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, false);
                foreach (DictionaryEntry language in res)
                {
                    _availableLanguages.Add(new Tuple<string, string>(language.Key.ToString(), language.Value.ToString()));
                }
            }
            return _availableLanguages;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            PhotoApp.Properties.Settings.Default.LastVersion = Version;
            PhotoApp.Properties.Settings.Default.Save();
            base.OnExit(e);
        }
    }
}
