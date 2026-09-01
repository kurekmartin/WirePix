using MediaDevices;
using PhotoApp.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using WirePix.Core.Import;
using WirePix.Core.Models;

namespace PhotoApp
{
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
        public static int DEVICE_UNKNOWN_STATUS = -1, DEVICE_CANNOT_CONNECT = 0, DEVICE_READY = 1;
        public static int DEVICE_FILES_READY = 1, DEVICE_FILES_SEARCHING = 0, DEVICE_FILES_ERROR = -1;
        public Device(MediaDevice mediaDevice)
        {
            _source = new MediaDevicesImportDevice(mediaDevice);
            var resources = System.Windows.Application.Current.Resources;
            _coordinator = new ImportCoordinator(resources[Properties.Keys.TempFolder].ToString(), resources[Properties.Keys.LogsFolder].ToString());
        }
        public string ID => _source.Id;
        public string Manufacturer => _source.Manufacturer;
        public string Name => _source.Name;
        public int Status { get { try { _source.Connect(); _source.Disconnect(); return DEVICE_READY; } catch { return DEVICE_CANNOT_CONNECT; } } }
        public int FileSearchStatus => _plan == null ? DEVICE_FILES_SEARCHING : (_plan.Files.Count > 0 ? DEVICE_FILES_READY : DEVICE_FILES_ERROR);
        public List<string> MediaDirectories => _source.GetDrives().Select(x => x.RootPath).ToList();
        public double[] Space { get { try { return _source.GetSpace(); } catch { return new[] { 0d, 0d, 0d }; } } }
        public List<string> FileTypes(BackgroundWorker worker, DoWorkEventArgs e)
        {
            if (_fileTypes != null) return _fileTypes;
            _fileTypes = new List<string>();
            try { _source.Connect(); foreach (var drive in _source.GetDrives()) CollectTypes(drive.RootPath); _source.Disconnect(); } catch { }
            return _fileTypes;
        }
        private void CollectTypes(string path)
        {
            foreach (var file in _source.EnumerateFiles(path) ?? Enumerable.Empty<ImportFile>()) { var ext = System.IO.Path.GetExtension(file.Name).ToUpperInvariant(); if (!_fileTypes.Contains(ext)) _fileTypes.Add(ext); }
            foreach (var child in _source.EnumerateDirectories(path) ?? Enumerable.Empty<string>()) CollectTypes(child);
        }
        public int FilesToDownload => FilesToCopyCount;
        public void GetFilesByDate(BackgroundWorker worker, DoWorkEventArgs e, DownloadSettings settings)
        {
            try { _plan = _coordinator.CreatePlanAsync(_source, settings, new Progress<ImportProgress>(p => Report(worker, p)), CancellationToken.None).GetAwaiter().GetResult(); FilesTotal = _plan.Files.Count; e.Result = new WorkerResult(MainWindow.RESULT_OK, TaskType.FindFiles); }
            catch (OperationCanceledException) { e.Cancel = true; }
            catch { e.Result = new WorkerResult(MainWindow.RESULT_ERROR, TaskType.FindFiles); }
        }
        public void CopyFiles(BackgroundWorker worker, DoWorkEventArgs e, DownloadSettings settings)
        {
            try
            {
                if (_plan == null || _plan.Date != settings.Date) GetFilesByDate(worker, e, settings);
                if (e.Cancel) return;
                var result = _coordinator.ExecuteAsync(_source, _plan, settings, new WirePix.Core.Naming.NamingContext(), new Progress<ImportProgress>(p => Report(worker, p))).GetAwaiter().GetResult();
                FilesDoneCount = result.FilesDone; Errors = result.Errors; FilesTotal = result.FilesTotal; e.Result = new WorkerResult(MainWindow.RESULT_OK, TaskType.CopyFiles);
            }
            catch (OperationCanceledException) { e.Cancel = true; }
            catch { e.Result = new WorkerResult(MainWindow.RESULT_ERROR, TaskType.CopyFiles); }
        }
        private static void Report(BackgroundWorker worker, ImportProgress p)
        {
            if (!worker.WorkerReportsProgress) return;
            var task = p.Stage switch { ImportStage.Searching => Properties.Resources.FileSearch, ImportStage.Filtering => Properties.Resources.DeviceFileFilterDate, ImportStage.Downloading => Properties.Resources.DeviceCopyingFiles, ImportStage.GeneratingThumbnail => Properties.Resources.DeviceGeneratingThumbnail, ImportStage.Deleting => Properties.Resources.DeviceDeletingFilesTask, _ => Properties.Resources.DeviceSortingFile };
            worker.ReportProgress(p.Total == 0 ? 0 : p.Completed * 100 / p.Total, new ProgressUpdateArgs { taskName = task, currentTask = p.CurrentFile ?? string.Empty, progressText = p.Total == 0 ? string.Empty : string.Format(Properties.Resources.DeviceFilesDoneCount, p.Completed, p.Total), indeterminateTask = p.Total == 0 });
        }
    }
}
