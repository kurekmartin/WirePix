# WirePix
A program for Windows designed to automate the download of files from a media device - mainly cameras (full support for smartphones is planned)

Main functions:
- download files based on date
- download new files since last backup
- sort and rename files based on specified folder structure
- create backup in selected folder
- create smaller images with given resolution
- check if downloaded files are not corrupted (slower download speed)
- delete successfully downloaded files from device
- save all settings for future use

Currently supported languages:
- English
- Czech

This is just initial release and there will be more features in future updates.

Download latest version https://github.com/KurekMartin/WirePix/releases/latest

**For feedback, please use the option that is listed directly in the program.**

![Main](https://user-images.githubusercontent.com/79570332/164648004-d3749cd4-3c7e-4b7e-bb43-5694332c95f1.png)

## Name structure
You can choose filename and folder structure to store your files. For example, by date, device manufacturer or create a folder with a custom name.
For the day and month format, you can choose a name in addition to the number, in both short and long form. e.g. April can be shown as 04, Apr or April.

You can switch the format type by selecting the tag and choosing from dropdown list.

## Thumbnails
Thumbnails are generated from all [supported image files](https://imagemagick.org/script/formats.php#supported).

You can specify size of shorter or longer side for the newly created images.

## Smartphone support
In the current version of the program you can also download files from your phone, but not only files from the camera are placed in the same folder (e.g. social media apps, etc.). In a future update there will be an option to select folders from which not to download.

## Planned features
- sort files based on file type
- possibility to download only specified file types
- choose exact files to download
- select custom color theme
- exclude selected folder from download (mainly for smartphones)
- download directly from SD card reader

If you have a suggestion for improvement you can use the feedback option in the program.

## Development

WirePix is a Windows-only WPF application targeting .NET 10 and x64. Building it requires a .NET 10 SDK. Visual Studio users should use Visual Studio 2026 with the .NET desktop development workload.

Restore and build the application from the repository root:

```powershell
dotnet restore PhotoApp/PhotoApp.csproj
dotnet build PhotoApp/PhotoApp.csproj --configuration Release --no-restore
```

The supported operating systems are x64 editions of Windows 10 version 1809 or later and Windows 11 that are supported by .NET 10. The current migration intentionally keeps the existing NuGet package versions; package modernization will be handled separately.

The legacy Visual Studio Installer project has not yet been migrated for .NET 10 and is excluded from default solution builds. Build and test the application project directly; do not use the existing installer for .NET 10 releases.
