using System;
using System.Windows;
using System.Windows.Controls;

namespace PhotoApp.Dialogs;

public partial class ChangelogDialog : UserControl
{
    public ChangelogDialog(double parentHeight,double parentWidth)
    {
        InitializeComponent();
        svChangeScrollView.MaxHeight = parentHeight*0.7;
        spChangelog.MaxWidth = parentWidth*0.8;
        tbVerison.Text = ((App)Application.Current).Version;
        LoadChangelog();
    }

    private void LoadChangelog()
    {
        foreach (string line in Properties.Resources.Changelog_NewFeatures_List.Split(Environment.NewLine.ToCharArray(),StringSplitOptions.RemoveEmptyEntries))
        {
            spNewFeatures.Children.Add(CreateItem(line));
        }
        if(spNewFeatures.Children.Count == 1) { spNewFeatures.Visibility = Visibility.Collapsed; }
        foreach (string line in Properties.Resources.Changelog_Fixes_List.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
        {
            spFixes.Children.Add(CreateItem(line));
        }
        if(spFixes.Children.Count == 1) { spFixes.Visibility = Visibility.Collapsed; }
    }

    private TextBlock CreateItem(string text)
    {
        TextBlock textBlock = new TextBlock();
        textBlock.Text = $"• {text}";
        textBlock.TextWrapping = TextWrapping.Wrap;
        textBlock.Style = FindResource("MaterialDesignBody2TextBlock") as Style;
        return textBlock;
    }
}