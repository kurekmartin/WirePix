using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PhotoApp.Dialogs;

/// <summary>
/// Interaction logic for UsedLibraries.xaml
/// </summary>
public partial class UsedLibraries : UserControl
{
    public UsedLibraries()
    {
        InitializeComponent();
    }

    private void btnMagickNET_Click(object sender, RoutedEventArgs e)
    {
        ShellLauncher.Open(target: "https://github.com/dlemstra/Magick.NET");
    }

    private void btnMaterialDesign_Click(object sender, RoutedEventArgs e)
    {
        ShellLauncher.Open(target: "https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit");
    }

    private void btnMetadataExtractor_Click(object sender, RoutedEventArgs e)
    {
        ShellLauncher.Open(target: "https://github.com/drewnoakes/metadata-extractor-dotnet");
    }

    private void btnOctokit_Click(object sender, RoutedEventArgs e)
    {
        ShellLauncher.Open(target: "https://github.com/octokit/octokit.net");
    }

    private void btnUsbEvents_Click(object sender, RoutedEventArgs e)
    {
        ShellLauncher.Open(target: "https://github.com/Jinjinov/Usb.Events");
    }

    private void btnMediaDevices_Click(object sender, RoutedEventArgs e)
    {
        ShellLauncher.Open(target: "https://github.com/Bassman2/MediaDevices");
    }
}