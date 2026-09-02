using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using PhotoApp.Properties;
using WirePix.Core.Import;
using WirePix.Core.Import.Contracts;
using WirePix.Core.Naming.Templates;
using WirePix.Devices.Contracts;
using WirePix.Devices.Models;

namespace PhotoApp;

public sealed class Device
{
    private IMediaDevice _sourceDevice;
    private readonly ImportCoordinator _coordinator;
    private ImportPlan _plan;
    private List<string> _fileTypes;
    public int FilesDoneCount { get; private set; }
    public int FilesTotal { get; private set; }
    public int Errors { get; private set; }
    public int FilesToCopyCount => _plan?.Files?.Count ?? 0;
    public const int DEVICE_UNKNOWN_STATUS = -1, DEVICE_CANNOT_CONNECT = 0, DEVICE_READY = 1;
    public const int DEVICE_FILES_READY = 1, DEVICE_FILES_SEARCHING = 0, DEVICE_FILES_ERROR = -1;

    public Device(IMediaDevice mediaDevice)
    {
        _sourceDevice = mediaDevice ?? throw new ArgumentNullException(paramName: nameof(mediaDevice));
        ResourceDictionary resources = Application.Current.Resources;
        _coordinator = new ImportCoordinator(
            tempFolder: resources[key: Keys.TempFolder].ToString(),
            logFolder: resources[key: Keys.LogsFolder].ToString());
    }

    public string Id => _sourceDevice.Id;
    public string Manufacturer => _sourceDevice.Manufacturer ?? string.Empty;
    public string Name => _sourceDevice.DisplayName;

    public int Status => _sourceDevice.Status.IsUsable
        ? DEVICE_READY
        : DEVICE_CANNOT_CONNECT;

    public int FileSearchStatus => _plan == null
        ? DEVICE_FILES_SEARCHING
        : _plan.Files.Count > 0
            ? DEVICE_FILES_READY
            : DEVICE_FILES_ERROR;

    public List<string> MediaDirectories =>
    [
        .. _sourceDevice.GetMediaSourcesAsync(CancellationToken.None)
                  .GetAwaiter()
                  .GetResult()
                  .Select(selector: source => source.DevicePath)
    ];

    public double[] Space
    {
        get
        {
            MediaDeviceStorage storage = _sourceDevice.Storage;
            if (storage == null)
            {
                return [0d, 0d, 0d];
            }

            const double bytesPerGigabyte = 1073741824d;
            return
            [
                Math.Round(value: storage.TotalBytes / bytesPerGigabyte, digits: 2),
                Math.Round(value: storage.AvailableBytes / bytesPerGigabyte, digits: 2),
                Math.Round(value: storage.UsedBytes / bytesPerGigabyte, digits: 2)
            ];
        }
    }

    public List<string> FileTypes(BackgroundWorker worker, DoWorkEventArgs e)
    {
        if (_fileTypes != null)
        {
            return _fileTypes;
        }

        try
        {
            _fileTypes =
            [
                .. _sourceDevice.GetMediaAsync(CancellationToken.None)
                          .GetAwaiter()
                          .GetResult()
                          .Select(selector: item => Path.GetExtension(item.FileName).ToUpperInvariant())
                          .Distinct()
            ];
        }
        catch
        {
            _fileTypes = [];
        }

        return _fileTypes;
    }

    public int FilesToDownload => FilesToCopyCount;

    internal void UpdateSource(IMediaDevice mediaDevice)
    {
        _sourceDevice = mediaDevice ?? throw new ArgumentNullException(paramName: nameof(mediaDevice));
    }

    public void GetFilesByDate(BackgroundWorker worker, DoWorkEventArgs e, DownloadSettings settings)
    {
        try
        {
            _plan = ImportCoordinator.CreatePlanAsync(
                    device: _sourceDevice,
                    settings: settings,
                    progress: new Progress<ImportProgress>(handler: progress => Report(worker: worker, p: progress)),
                    cancellationToken: CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            FilesTotal = _plan.Files.Count;
            e.Result = new WorkerResult(code: MainWindow.RESULT_OK, task: TaskType.FindFiles);
        }
        catch (OperationCanceledException)
        {
            e.Cancel = true;
        }
        catch
        {
            e.Result = new WorkerResult(code: MainWindow.RESULT_ERROR, task: TaskType.FindFiles);
        }
    }

    public void CopyFiles(BackgroundWorker worker, DoWorkEventArgs e, DownloadSettings settings)
    {
        try
        {
            if (_plan == null || _plan.Date != settings.Date)
            {
                GetFilesByDate(worker: worker, e: e, settings: settings);
            }

            if (e.Cancel)
            {
                return;
            }

            ImportResult result = _coordinator.ExecuteAsync(
                    device: _sourceDevice,
                    plan: _plan,
                    settings: settings,
                    naming: new NamingContext
                    {
                        TagLanguage = Settings.Default.TagLanguage,
                        UseTagLanguage = Settings.Default.UseDifferentLangForTags
                    },
                    progress: new Progress<ImportProgress>(handler: progress => Report(worker: worker, p: progress)))
                .GetAwaiter()
                .GetResult();
            FilesDoneCount = result.FilesDone;
            Errors = result.Errors;
            FilesTotal = result.FilesTotal;
            e.Result = new WorkerResult(code: MainWindow.RESULT_OK, task: TaskType.CopyFiles);
        }
        catch (OperationCanceledException)
        {
            e.Cancel = true;
        }
        catch
        {
            e.Result = new WorkerResult(code: MainWindow.RESULT_ERROR, task: TaskType.CopyFiles);
        }
    }

    private static void Report(BackgroundWorker worker, ImportProgress p)
    {
        if (!worker.WorkerReportsProgress)
        {
            return;
        }

        string task = p.Stage switch
        {
            ImportStage.Searching => Resources.FileSearch,
            ImportStage.Filtering => Resources.DeviceFileFilterDate,
            ImportStage.Downloading => Resources.DeviceCopyingFiles,
            ImportStage.GeneratingThumbnail => string.Format(
                format: Resources.DeviceGeneratingThumbnail,
                arg0: p.CurrentFile ?? string.Empty),
            ImportStage.Deleting => Resources.DeviceDeletingFilesTask,
            _ => Resources.DeviceSortingFile
        };
        worker.ReportProgress(
            percentProgress: p.Total == 0 ? 0 : p.Completed * 100 / p.Total,
            userState: new ProgressUpdateArgs
            {
                taskName = task,
                currentTask = p.CurrentFile ?? string.Empty,
                progressText = p.Total == 0
                    ? string.Empty
                    : string.Format(
                        format: Resources.DeviceFilesDoneCount,
                        arg0: p.Completed,
                        arg1: p.Total),
                indeterminateTask = p.Total == 0
            });
    }
}
