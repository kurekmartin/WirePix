using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace PhotoApp.Dialogs;

/// <summary>
/// Interakční logika pro ErrorDialog.xaml
/// </summary>
public partial class AppSettingsDialog : UserControl
{
    private MainWindow mainWindow;
    private List<Tuple<string, string>> languages = new();
    private string origLanguage;

    public AppSettingsDialog(MainWindow window)
    {
        DataContext = Properties.Settings.Default;
        InitializeComponent();
        mainWindow = window;
        languages = App.GetAvailableLanguages();
        cbLanguages.ItemsSource = cbTagLanguages.ItemsSource = languages.ConvertAll(converter: x => x.Item2);
        cbLanguages.SelectedIndex = cbLanguages.Items.IndexOf(item: languages.Find(match: x => x.Item1 == Properties.Settings.Default.Language).Item2);
        cbTagLanguages.SelectedIndex = cbTagLanguages.Items.IndexOf(item: languages.Find(match: x => x.Item1 == Properties.Settings.Default.TagLanguage).Item2);
        origLanguage = Properties.Settings.Default.Language;
    }

    private void cbDarkMode_Changed(object sender, RoutedEventArgs e)
    {
        var checkBox = (CheckBox)sender;
        App.SetThemeMode(darkMode: (bool)checkBox.IsChecked);
    }

    private void btnOK_Click(object sender, RoutedEventArgs e)
    {
        mainWindow.DialogClose(sender: this, result: null);
    }

    private void cbCheckNewVersion_Checked(object sender, RoutedEventArgs e)
    {
        var checkBox = (CheckBox)sender;
        Properties.Settings.Default.CheckUpdateOnStartup = (bool)checkBox.IsChecked;
        Properties.Settings.Default.Save();
    }

    private void cbLanguages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.RemovedItems.Count > 0)
        {
            Properties.Settings.Default.Language = languages.Find(match: x => x.Item2 == (string)e.AddedItems[index: 0]).Item1;
            Properties.Settings.Default.Save();

            if (origLanguage != Properties.Settings.Default.Language)
            {
                tbInfo.Text = Properties.Resources.SettingsInfoRestart;
                dpInfo.Visibility = btnRestart.Visibility = Visibility.Visible;
            }
            else
            {
                dpInfo.Visibility = btnRestart.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void btnRestart_Click(object sender, RoutedEventArgs e)
    {
        string currentExecutablePath = Process.GetCurrentProcess().MainModule.FileName;
        ShellLauncher.Open(target: currentExecutablePath);
        Application.Current.Shutdown();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        MaxWidth = ActualWidth * 1.2;
    }

    private void cbTagLanguages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.RemovedItems.Count > 0)
        {
            Properties.Settings.Default.TagLanguage = languages.Find(match: x => x.Item2 == (string)e.AddedItems[index: 0]).Item1;
            Properties.Settings.Default.Save();
        }
    }

    private void cbDifferentLanguageForTags_Checked(object sender, RoutedEventArgs e)
    {
        var checkBox = (CheckBox)sender;
        Properties.Settings.Default.UseDifferentLangForTags = (bool)checkBox.IsChecked;
        Properties.Settings.Default.Save();
    }
}