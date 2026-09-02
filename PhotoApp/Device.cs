using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using MediaDevices;
using PhotoApp.Devices;
using PhotoApp.Properties;
using WirePix.Core.Import;
using WirePix.Core.Import.Contracts;
using WirePix.Core.Naming.Templates;

namespace PhotoApp;

public sealed class Device
{
    private readonly MediaDevicesImportDevice _source;
    private readonly ImportCoordinator _coordinator;
    private ImportPlan _plan;
    private List<string> _fileTypes;
    public int FilesDoneCount { get; private set; }
    public int FilesTotal { get; private set; }
    public int Errors { get; private set; }
    public int FilesToCopyCount => _plan?.Files?.Count ?? 0;
    public const int DEVICE_UNKNOWN_STATUS = -1, DEVICE_CANNOT_CONNECT = 0, DEVICE_READY = 1;
    public const int DEVICE_FILES_READY = 1, DEVICE_FILES_SEARCHING = 0, DEVICE_FILES_ERROR = -1;

    public Device(MediaDevice mediaDevice)
    {
        _source = new MediaDevicesImportDevice(device: mediaDevice);
        ResourceDictionary resources = Application.Current.Resources;
        _coordinator = new ImportCoordinator(tempFolder: resources[key: Keys.TempFolder].ToString(), logFolder: resources[key: Keys.LogsFolder].ToString());
    }

    public string Id => _source.Id;
    public string Manufacturer => _source.Manufacturer;
    public string Name => _source.Name;

    public int Status
    {
        get
        {
            try
            {
                _source.Connect();
                _source.Disconnect();
                return DEVICE_READY;
            }
            catch
            {
                return DEVICE_CANNOT_CONNECT;
            }
        }
    }

    public int FileSearchStatus => _plan == null ? DEVICE_FILES_SEARCHING : _plan.Files.Count > 0 ? DEVICE_FILES_READY : DEVICE_FILES_ERROR;

    public List<string> MediaDirectories =>
    [
        .. _source.GetDrives()
                  .Select(selector: x => x.RootPath)
    ];

    public double[] Space
    {
        get
        {
            try
            {
                return _source.GetSpace();
            }
            catch
            {
                return [0d, 0d, 0d];
            }
        }
    }

    public List<string> FileTypes(BackgroundWorker worker, DoWorkEventArgs e)
    {
        if (_fileTypes != null)
        {
            return _fileTypes;
        }

        _fileTypes = [];
        try
        {
            _source.Connect();
            foreach (ImportDrive drive in _source.GetDrives())
            {
                CollectTypes(path: drive.RootPath);
            }

            _source.Disconnect();
        }
        catch
        {
            // ignored
        }

        return _fileTypes;
    }

    private void CollectTypes(string path)
    {
        foreach (ImportFile file in _source.EnumerateFiles(path: path) ?? [])
        {
            string ext = Path.GetExtension(path: file.Name).ToUpperInvariant();
            if (!_fileTypes.Contains(item: ext))
            {
                _fileTypes.Add(item: ext);
            }
        }

        foreach (string child in _source.EnumerateDirectories(path: path) ?? [])
        {
            CollectTypes(path: child);
        }
    }

    public int FilesToDownload => FilesToCopyCount;

    public void GetFilesByDate(BackgroundWorker worker, DoWorkEventArgs e, DownloadSettings settings)
    {
        try
        {
            _plan = ImportCoordinator.CreatePlanAsync(device: _source, settings: settings, progress: new Progress<ImportProgress>(handler: p => Report(worker: worker, p: p)), cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
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

            ImportResult result = _coordinator.ExecuteAsync(device: _source, plan: _plan, settings: settings, naming: new NamingContext(), progress: new Progress<ImportProgress>(handler: p => Report(worker: worker, p: p))).GetAwaiter().GetResult();
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
            ImportStage.GeneratingThumbnail => Resources.DeviceGeneratingThumbnail,
            ImportStage.Deleting => Resources.DeviceDeletingFilesTask,
            _ => Resources.DeviceSortingFile
        };
        worker.ReportProgress(percentProgress: p.Total == 0
                ? 0
                : p.Completed * 100 / p.Total,
            userState: new ProgressUpdateArgs
            {
                taskName = task,
                currentTask = p.CurrentFile ?? string.Empty,
                progressText = p.Total == 0
                    ? string.Empty
                    : string.Format(format: Resources.DeviceFilesDoneCount,
                        arg0: p.Completed,
                        arg1: p.Total),
                indeterminateTask = p.Total == 0
            });
    }
}