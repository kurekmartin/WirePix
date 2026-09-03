using System.Collections.Generic;
using System.IO;
using WirePix.Core.Models.Devices;
using WirePix.Core.Models.Settings;
using WirePix.Devices.Contracts;

namespace WirePix.Core.Validation;

public enum ImportValidationCode
{
    DeviceNotSelected,
    DeviceNameMissing,
    RootMissing,
    RootNotFound,
    BackupMissing,
    BackupNotFound,
    FolderTemplateMissing,
    FileTemplateMissing,
    ThumbnailDirectoryMissing,
    ThumbnailDirectoryNotFound,
    ThumbnailSizeInvalid,
    DateRangeInvalid
}

public readonly record struct ImportValidationIssue(ImportValidationCode Code, string Field);

public static class ImportSettingsValidator
{
    public static IReadOnlyList<ImportValidationIssue> Validate(
        IMediaDevice device,
        DeviceInfo deviceInfo,
        DownloadSettings settings)
    {
        var issues = new List<ImportValidationIssue>();
        if (device == null)
        {
            issues.Add(new ImportValidationIssue(ImportValidationCode.DeviceNotSelected, "Device"));
        }

        if (string.IsNullOrWhiteSpace(deviceInfo?.Name))
        {
            issues.Add(new ImportValidationIssue(ImportValidationCode.DeviceNameMissing, "DeviceName"));
        }

        ValidateDirectory(settings?.Paths?.Root, "Root", ImportValidationCode.RootMissing, ImportValidationCode.RootNotFound, issues);
        if (settings is { Backup: true, Paths: not null })
        {
            ValidateDirectory(settings.Paths.Backup, "Backup", ImportValidationCode.BackupMissing, ImportValidationCode.BackupNotFound, issues);
        }

        if (settings?.Paths?.FolderTags == null || settings.Paths.FolderTags.Count == 0)
        {
            issues.Add(new ImportValidationIssue(ImportValidationCode.FolderTemplateMissing, "FolderTemplate"));
        }

        if (settings?.Paths?.FileTags == null || settings.Paths.FileTags.Count == 0)
        {
            issues.Add(new ImportValidationIssue(ImportValidationCode.FileTemplateMissing, "FileTemplate"));
        }

        if (settings?.Thumbnail == true)
        {
            if (settings.Paths != null)
            {
                ValidateDirectory(settings.Paths.Thumbnail, "ThumbnailDirectory", ImportValidationCode.ThumbnailDirectoryMissing, ImportValidationCode.ThumbnailDirectoryNotFound, issues);
            }

            if (settings.ThumbnailSettings is not { Value: > 0 })
            {
                issues.Add(new ImportValidationIssue(ImportValidationCode.ThumbnailSizeInvalid, "ThumbnailSize"));
            }
        }

        if (settings?.Date != null && settings.Date.Start > settings.Date.End)
        {
            issues.Add(new ImportValidationIssue(ImportValidationCode.DateRangeInvalid, "DateRange"));
        }

        return issues;
    }

    private static void ValidateDirectory(string path, string field, ImportValidationCode missing, ImportValidationCode notFound, List<ImportValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            issues.Add(new ImportValidationIssue(missing, field));
        }
        else if (!Directory.Exists(path))
        {
            issues.Add(new ImportValidationIssue(notFound, field));
        }
    }
}