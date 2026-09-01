using ImageMagick;
using ImageMagick.Formats;
using MediaDevices;
using PhotoApp.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PhotoApp
{
    public class Device
    {
        private struct CopyItem
        {
            public CopyItem(string tmpFile, string fullDestPath, byte[] origHash, string origFile)
            {
                this.tmpFile = tmpFile;
                this.fullDestPath = fullDestPath;
                this.origHash = origHash;
                this.origFile = origFile;
            }
            public string tmpFile;
            public string fullDestPath;
            public byte[] origHash;
            public string origFile;

        }

        public int FilesDoneCount { get; private set; } = 0;
        public int FilesTotal { get; private set; } = 0;
        public int Errors { get; private set; } = 0;

        private readonly MediaDevice _device;
        private List<MediaDirectoryInfo> _mediaDirList;
        private double[] _space;
        private IEnumerable<MediaFileInfo> filesToCopy;
        private IEnumerable<MediaFileInfo> allFiles;
        private bool fileSearchDone = false;
        private List<string> fileTypes = new List<string>();

        public int FilesToCopyCount { get; private set; } = 0;
        private double sizeToProcess;
        private DownloadSettings _lastSettings;

        private bool fileCheckDone = false;

        private static readonly int maxAttempts = 5;

        //device status
        public static int DEVICE_UNKNOWN_STATUS = -1;
        public static int DEVICE_CANNOT_CONNECT = 0;
        public static int DEVICE_READY = 1;

        //files status
        public static int DEVICE_FILES_READY = 1;
        public static int DEVICE_FILES_SEARCHING = 0;
        public static int DEVICE_FILES_ERROR = -1;

        private readonly Log _log = new Log();

        private readonly object _lockLog = new object();
        private readonly object _lockReport = new object();


        //public Device() { }
        public Device(MediaDevice mediaDevice)
        {
            _device = mediaDevice;
        }

        public string ID
        {
            get
            {
                if (_device != null)
                {
                    return _device.DeviceId;
                }
                return string.Empty;
            }
        }
        public int Status
        {
            get
            {
                if (_device != null)
                {
                    try
                    {
                        _device.Connect();
                    }
                    catch
                    {
                        return DEVICE_CANNOT_CONNECT;
                    }
                    _device.Disconnect();
                    return DEVICE_READY;
                }
                return DEVICE_UNKNOWN_STATUS;
            }
        }

        public int FileSearchStatus
        {
            get
            {
                if (!fileSearchDone)
                {
                    return DEVICE_FILES_SEARCHING;
                }
                else if (fileSearchDone && allFiles.Count() > 0)
                {
                    return DEVICE_FILES_READY;
                }
                else
                {
                    return DEVICE_FILES_ERROR;
                }
            }
        }

        public string Manufacturer
        {
            get
            {
                string manufacturer = _device.Manufacturer;
                return manufacturer;
            }
        }

        public string Name
        {
            get
            {
                if (_device != null)
                {
                    string name = _device.Description;
                    return name;
                }
                else
                    return string.Empty;
            }
        }

        //uloziste [0]-celkove [1]-volne [2]-obsazene
        public double[] Space
        {
            get
            {
                if (_space == null)
                {
                    _device.Connect();
                    _space = new double[3];
                    var drives = _device.GetDrives();
                    foreach (MediaDriveInfo drive in drives)
                    {
                        _space[0] += drive.TotalSize / (double)1073741824; //prevod na GB
                        _space[1] += drive.AvailableFreeSpace / (double)1073741824;
                    }
                    _device.Disconnect();
                    _space = _space.Select(x => Math.Round(x, 2)).ToArray();
                    _space[2] = Math.Round(_space[0] - _space[1], 2);
                }
                return _space;
            }
        }

        public List<string> FileTypes(BackgroundWorker worker, DoWorkEventArgs e)
        {
            if (fileTypes != null)
            {
                return fileTypes;
            }
            IEnumerable<MediaFileInfo> mediaFiles = filesToCopy;
            if (mediaFiles == null)
            {
                GetAllFiles(worker, e);
            }

            return fileTypes;
        }

        public int FilesToDownload
        {
            get
            {
                _device.Connect();
                int c = filesToCopy.Count();
                _device.Disconnect();
                return c;
            }

        }

        //hledani DCIM
        public List<string> MediaDirectories
        {
            get
            {
                if (_mediaDirList == null)
                {
                    _device.Connect();
                    var drives = _device.GetDrives();
                    _mediaDirList = new List<MediaDirectoryInfo>();
                    foreach (MediaDriveInfo drive in drives)
                    {
                        MediaDirectoryInfo root = drive.RootDirectory;
                        if (root != null)
                        {
                            GetMediaDirectory(_device, root, _mediaDirList);
                        }
                    }
                    _device.Disconnect();
                }
                return _mediaDirList.Select(dir => { return dir.FullName; }).ToList();
            }

        }

        private void GetMediaDirectory(MediaDevice device, MediaDirectoryInfo dir, List<MediaDirectoryInfo> mediaDirList)
        {
            var dirs = dir.EnumerateDirectories();
            var mediaDir = dirs.FirstOrDefault(d => d.Name == "DCIM");

            //pokud nalezeno, dale nehledat
            if (mediaDir != null)
            {
                mediaDirList.Add(mediaDir);
                return;
            }

            //pokud nenalezeno a aktualni adresar obsahuje dalsi slozky, hledat dale
            if (dirs != null)
            {
                foreach (MediaDirectoryInfo d in dirs)
                {
                    GetMediaDirectory(device, d, mediaDirList);
                }
            }
            return;
        }

        private void GetAllFiles(BackgroundWorker worker, DoWorkEventArgs e, ProgressUpdateArgs progressArgs = new ProgressUpdateArgs())
        {
            allFiles = Enumerable.Empty<MediaFileInfo>();
            foreach (string dir in MediaDirectories)
            {
                allFiles = allFiles.Concat(GetAllFilesList(dir, worker, e, progressArgs));
            }
        }

        //nalezeni souboru dle data
        public void GetFilesByDate(BackgroundWorker worker, DoWorkEventArgs e, DownloadSettings settings)
        {
            bool _checkDateRange;
            ProgressUpdateArgs progressArgs = new ProgressUpdateArgs
            {
                taskName = Properties.Resources.FileSearch,
                indeterminateTask = true
            };
            worker.ReportProgress(0, progressArgs);

            if (fileCheckDone && _lastSettings.Date == settings.Date)
            {
                e.Result = new WorkerResult(MainWindow.RESULT_OK, TaskType.FindFiles);
                return;
            }

            fileCheckDone = false;
            filesToCopy = Enumerable.Empty<MediaFileInfo>();
            IEnumerable<MediaFileInfo> allFiles = Enumerable.Empty<MediaFileInfo>();


            if (settings.Date.Start == new DateTime())
            {
                _checkDateRange = false;
            }
            else
            {
                settings.Date.End = settings.Date.End.AddTicks(-1).AddDays(1); // nastavení konce dne
                _checkDateRange = true;
            }

            _device.Connect();

            if (!_log.Running) { _log.Start(); }

            FilesTotal = 0;
            //hledani souboru ve vsech DCIM slozkach
            try
            {
                //GetAllFiles(worker, e, progressArgs);
                foreach (MediaDirectoryInfo dir in _mediaDirList)
                {
                    allFiles = allFiles.Concat(GetAllFilesList(dir.FullName, worker, e, progressArgs));
                }
                if (_checkDateRange)
                {
                    progressArgs.taskName = Properties.Resources.DeviceFileFilterDate;
                    worker.ReportProgress(0, progressArgs);

                    filesToCopy = allFiles.Where(f => ((DateTime)f.CreationTime) >= settings.Date.Start && ((DateTime)f.CreationTime) <= settings.Date.End);
                }
                else
                {
                    filesToCopy = allFiles.ToList();
                }

                FilesToCopyCount = filesToCopy.Count();
                sizeToProcess = filesToCopy.Sum(f => Convert.ToDouble(f.Length / 1048576));
            }
            catch (Exception ex)
            {
                var st = new StackTrace();
                var sf = st.GetFrame(0);
                var currentMethodName = sf.GetMethod();

                _log.Add(ex.ToString(), LogType.ERROR, currentMethodName.Name);
                _log.Stop();

                FilesToCopyCount = 0;
                sizeToProcess = 0;

                e.Result = new WorkerResult(MainWindow.RESULT_ERROR, TaskType.FindFiles);

                _device.Disconnect();
                return;
            }

            _device.Disconnect();
            _lastSettings = new DownloadSettings();
            _lastSettings.Date.Start = settings.Date.Start;
            _lastSettings.Date.End = settings.Date.End;
            fileCheckDone = true;
            e.Result = new WorkerResult(MainWindow.RESULT_OK, TaskType.FindFiles);
        }


        //rekurzivni prohledani vsech slozek v adresari
        //vynechani slozek zacinajicich '.'
        private IEnumerable<MediaFileInfo> GetAllFilesList(string path, BackgroundWorker worker, DoWorkEventArgs e, ProgressUpdateArgs progressArgs)
        {
            IEnumerable<MediaFileInfo> files = Enumerable.Empty<MediaFileInfo>();
            if (worker.CancellationPending)
            {
                e.Cancel = true;
                FilesTotal = 0;
                FilesToCopyCount = 0;
                filesToCopy = Enumerable.Empty<MediaFileInfo>();
            }
            else
            {
                if (worker.WorkerReportsProgress)
                {
                    progressArgs.currentTask = path;
                    progressArgs.progressText = string.Format(Properties.Resources.DeviceFilesFound, FilesTotal);
                    worker.ReportProgress(0, progressArgs);
                }

                if (!_log.Running) { _log.Start(); }
                try
                {
                    //ziskani nazvu vsech souboru ve slozce
                    MediaDirectoryInfo dirInfo = _device.GetDirectoryInfo(path);
                    files = dirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly);

                    FilesTotal += files.Count();
                    if (worker.WorkerReportsProgress)
                    {
                        progressArgs.progressText = string.Format(Properties.Resources.DeviceFilesFound, FilesTotal);
                        worker.ReportProgress(0, progressArgs);
                    }


                    //prohledani podslozek
                    IEnumerable<string> subdirs = _device.EnumerateDirectories(path);
                    foreach (string dir in subdirs)
                    {
                        if (!dir.ElementAt(dir.LastIndexOf('\\') + 1).Equals('.')) // vynechani slozek zacinajicich "."
                        {
                            var currentFiles = GetAllFilesList(dir, worker, e, progressArgs);
                            files = files.Concat(currentFiles);

                            //získat typy souborů
                            currentFiles.ToList().ForEach(x =>
                            {
                                string ext = Path.GetExtension(x.Name).ToUpper();
                                if (!fileTypes.Contains(ext)) { fileTypes.Add(ext); }
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    var st = new StackTrace();
                    var sf = st.GetFrame(0);
                    var currentMethodName = sf.GetMethod();

                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
                    _log.Add(ex.ToString(), LogType.ERROR, currentMethodName.Name);
                    _log.Stop();
                    return Enumerable.Empty<MediaFileInfo>();
                }
            }
            return files;
        }

        public async void CopyFiles(BackgroundWorker worker, DoWorkEventArgs e, DownloadSettings settings)
        {
            List<string> filesDone = new List<string>();
            object _lockFilesDone = new object();

            if (FilesToCopyCount == 0 && _lastSettings == settings)
            {
                e.Result = new WorkerResult(MainWindow.RESULT_OK, TaskType.CopyFiles);
                return;
            }
            else
            {
                GetFilesByDate(worker, e, settings);
            }

            var st = new StackTrace();
            var sf = st.GetFrame(0);
            var currentMethodName = sf.GetMethod();


            ProgressUpdateArgs progressArgs = new ProgressUpdateArgs();
            FilesDoneCount = 0;
            byte[] origHash = null;
            double sizeCopied = 0;
            string tmpFolder = Application.Current.Resources[Properties.Keys.TempFolder].ToString();
            string tmpFile = "";
            BlockingCollection<CopyItem> filesCollection = new BlockingCollection<CopyItem>();

            if (!_log.Running) { _log.Start(); }

            Directory.CreateDirectory(tmpFolder);

            DateTime start = DateTime.Now;
            FilesDoneCount = 0;
            Errors = 0;

            MainWindow.ClearTemp();

            IEnumerable<MediaFileInfo> sortedFiles = filesToCopy.OrderBy(f => f.CreationTime).ThenBy(f => f.Name);

            var addItems = Task.Run(() =>
            {
                _device.Connect();
                int i = 0;
                foreach (MediaFileInfo file in sortedFiles)
                {
                    try
                    {
                        progressArgs.taskName = Properties.Resources.DeviceCopyingFiles;
                        progressArgs.indeterminateTask = false;


                        lock (_lockReport)
                        {
                            progressArgs.progressText = string.Format(Properties.Resources.DeviceFilesDoneCount, FilesDoneCount, FilesToCopyCount);
                            progressArgs.currentTask = String.Format(Properties.Resources.DeviceDownloadingFile, file.FullName);
                            worker.ReportProgress(FilesDoneCount * 100 / FilesToCopyCount, progressArgs);
                        }

                        //timer.Start();
                        if (worker.CancellationPending)
                        {
                            e.Cancel = true;
                            filesCollection.CompleteAdding();
                            _device.Disconnect();
                            return;
                        }
                        else
                        {

                            // stažení souboru do složky tmp -> nutné stáhnout pouze 1x (stahovani ze zarizeni je pomalé)
                            // stažení je nutné - přes knihovnu MediaDevices není možné číst Exif data
                            // info o souboru z knihovny MediaDevices poskytuje CreationTime 
                            // - !neodpovídá času pořízení fotky (EXIF) při delším zpracování fotografie (dlouhá expozice nebo kontinuální snímání)
                            string GUID = Guid.NewGuid().ToString();
                            tmpFile = Path.Combine(tmpFolder, GUID);
                            if (settings.CheckFiles)
                            {
                                origHash = GetFileHash(file.OpenRead());
                            }

                            for (i = 0; i < maxAttempts; i++)
                            {
                                _device.DownloadFileFromPersistentUniqueId(file.PersistentUniqueId, tmpFile);
                                var thread = Thread.CurrentThread;
                                Console.WriteLine($"[{thread.ManagedThreadId}] downloaded file {file.Name}");

                                if (!settings.CheckFiles || CheckFileHash(origHash, tmpFile))
                                {
                                    break;
                                }
                            }

                            // soubor se nepodařilo stáhnout ze zařízení -> další soubor
                            if (i == maxAttempts)
                            {
                                string message = $"could not download file {file.FullName}";
                                lock (_lockLog)
                                {
                                    _log.Add(message, LogType.ERROR, currentMethodName.Name);
                                }

                                if (File.Exists(tmpFile)) { File.Delete(tmpFile); }
                                continue;
                            }

                            if (!settings.CheckFiles) //hash pro ověření náhledu
                            {
                                origHash = GetFileHash(File.OpenRead(tmpFile));
                            }

                            // dosazení hodnot za tagy
                            string folder = Tags.TagsToValues(settings.Paths.FolderTags, this, file, tmpFile);
                            string fileName = Tags.TagsToValues(settings.Paths.FileTags, this, file, tmpFile);
                            fileName += Path.GetExtension(file.Name);
                            string destFullName = Path.Combine(folder, fileName);

                            CopyItem item = new CopyItem(tmpFile, destFullName, origHash, file.FullName); //přidat číslo (pořadí) souboru
                            filesCollection.Add(item);

                            i = 0;
                            int wait = 1000;
                            while (!_device.IsConnected && i < maxAttempts)
                            {
                                _device.Connect();
                                Thread.Sleep(wait);
                                wait *= 2;
                            }

                            if (!_device.IsConnected)
                            {
                                e.Result = new WorkerResult(MainWindow.RESULT_ERROR, TaskType.CopyFiles);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (_lockLog)
                        {
                            Errors++;
                            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
                            string message = $"File {file.FullName}\n\t{ex.ToString().Replace(Environment.NewLine, Environment.NewLine + "\t")}";
                            _log.Add(ex.ToString(), LogType.ERROR, currentMethodName.Name);
                            _log.Stop();
                        }
                        if (File.Exists(tmpFile)) { File.Delete(tmpFile); }
                    }
                }
                filesCollection.CompleteAdding();
            });

            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling(Environment.ProcessorCount * 1.0)) };

            //zpracování stažených souborů
            Parallel.ForEach(filesCollection.GetConsumingEnumerable(), options, (item, state) =>
              {
                  var thread = Thread.CurrentThread;
                  if (worker.CancellationPending)
                  {
                      e.Cancel = true;
                      state.Stop();
                  }

                  if (settings.Thumbnail)
                  {
                      lock (_lockReport)
                      {
                          progressArgs.currentTask = string.Format(Properties.Resources.DeviceGeneratingThumbnail, item.origFile);
                          worker.ReportProgress(FilesDoneCount * 100 / FilesToCopyCount, progressArgs);
                      }
                      GenerateThumbnail(settings, item.fullDestPath, item.tmpFile, item.origFile, BitConverter.ToString(item.origHash).Replace("-", string.Empty).ToLowerInvariant());
                  }


                  lock (_lockReport)
                  {
                      progressArgs.currentTask = string.Format(Properties.Resources.DeviceSortingFile, item.origFile);
                      worker.ReportProgress(FilesDoneCount * 100 / FilesToCopyCount, progressArgs);
                  }
                  bool successSave = SaveFiles(settings, item.fullDestPath, item.tmpFile, item.origFile, item.origHash);

                  if (settings.DeleteFiles && successSave)
                  {
                      lock (_lockFilesDone)
                      {
                          filesDone.Add(item.origFile);
                          Console.WriteLine($"[{thread.ManagedThreadId}] file {item.origFile} added to delete");
                      }

                  }

                  double size = 0;
                  if (File.Exists(item.tmpFile))
                  {
                      size = new FileInfo(item.tmpFile).Length / 1048576;
                      File.Delete(item.tmpFile);
                  }

                  DateTime end = DateTime.Now;
                  TimeSpan timeTotal = end - start;

                  lock (_lockReport)
                  {
                      sizeCopied += size;
                      double sizeRemain = sizeToProcess - sizeCopied;
                      FilesDoneCount++;
                      progressArgs.progressText = string.Format(Properties.Resources.DeviceFilesDoneCount, FilesDoneCount, FilesToCopyCount);
                      TimeSpan timeRemain = TimeSpan.FromTicks((long)(timeTotal.Ticks / sizeCopied) * (long)sizeRemain);
                      progressArgs.timeRemain = timeRemain;
                      worker.ReportProgress(FilesDoneCount * 100 / FilesToCopyCount, progressArgs);
                  }
              });

            await addItems;

            DeleteFiles(filesDone, worker);

            _log.Stop();
            _device.Disconnect();
            e.Result = new WorkerResult(MainWindow.RESULT_OK, TaskType.CopyFiles);
        }

        private void DeleteFiles(List<string> files, BackgroundWorker worker)
        {
            DateTime start = DateTime.Now;
            int totalCount = files.Count();
            int doneCount = 0;
            int progressPercent = 0;
            ProgressUpdateArgs progressArgs = new ProgressUpdateArgs
            {
                taskName = Properties.Resources.DeviceDeletingFilesTask,
                progressText = string.Format(Properties.Resources.DeviceDeletingProgress, doneCount, totalCount)
            };
            worker.ReportProgress(progressPercent, progressArgs);
            foreach (string file in files)
            {
                progressArgs.currentTask = string.Format(Properties.Resources.DeviceDeletingFile, file);
                worker.ReportProgress(progressPercent, progressArgs);
                _device.DeleteFile(file);
                Console.WriteLine($"file {file} deleted");
                Console.WriteLine($"file {file} exists: {_device.FileExists(file)}");

                doneCount++;
                progressPercent = doneCount * 100 / totalCount;
                DateTime end = DateTime.Now;
                TimeSpan timeTotal = end - start;
                TimeSpan timeRemain = TimeSpan.FromTicks((timeTotal.Ticks / doneCount) * (totalCount - doneCount));

                progressArgs.progressText = string.Format(Properties.Resources.DeviceDeletingProgress, doneCount, totalCount);
                progressArgs.timeRemain = timeRemain;
                worker.ReportProgress(progressPercent, progressArgs);
            }

        }

        private void GenerateThumbnail(DownloadSettings settings, string filePath, string tmpFile, string origFile, string origHash, int attempt = 0, int wait = 2000)
        {

            var st = new StackTrace();
            var sf = st.GetFrame(0);
            var currentMethodName = sf.GetMethod();

            if (!IsImage(origFile))
            {
                string message = $"Could not generate thumbnail for [{origFile}]. Not supported image format.";
                lock (_lockLog)
                {
                    _log.Add(message, LogType.INFO, currentMethodName.Name);
                }
                return;
            }

            try
            {
                // filename in format: filename(ORIG_EXT).jpg
                string outFile = Path.Combine(settings.Paths.Thumbnail, filePath);
                outFile = Path.Combine(Path.GetDirectoryName(outFile), $"{Path.GetFileNameWithoutExtension(outFile)}({Path.GetExtension(outFile).Remove(0, 1)}).jpg");

                bool cont = false;
                if (File.Exists(outFile))
                {
                    using (MagickImage image = new MagickImage(outFile))
                    {
                        if (image.Comment == origHash)
                        {
                            switch (settings.ThumbnailSettings.Selected)
                            {
                                case ThumbnailSelect.longerSide:
                                    if (settings.ThumbnailSettings.Value == Math.Max(image.Width, image.Height)) { cont = true; }
                                    break;
                                case ThumbnailSelect.shorterSide:
                                    if (settings.ThumbnailSettings.Value == Math.Min(image.Width, image.Height)) { cont = true; }
                                    break;
                            }
                        }
                        else
                        {
                            string newFileName = GetUniqueFileName(outFile, tmpFile, true);
                            if (newFileName == outFile)
                            {
                                cont = true;
                            }
                            else
                            {
                                outFile = newFileName;
                            }
                        }
                    }
                }

                if (cont)
                {
                    string message = $"Thumbnail already exists for file [{origFile}] in [{outFile}]";
                    lock (_lockLog)
                    {
                        _log.Add(message, LogType.INFO, currentMethodName.Name);
                    }
                    return;
                }

                var size = new MagickGeometry((uint)settings.ThumbnailSettings.Value);
                switch (settings.ThumbnailSettings.Selected)
                {
                    case ThumbnailSelect.longerSide:
                        size.FillArea = false;
                        break;
                    case ThumbnailSelect.shorterSide:
                        size.FillArea = true;
                        break;
                }

                var readSettings = new MagickReadSettings
                {
                    Defines = new DngReadDefines
                    {
                        ReadThumbnail = true,
                        UseCameraWhiteBalance = true,
                    }
                };

                bool thumbnailCreated = false;
                using (var image = new MagickImage())
                {
                    image.Ping(tmpFile, readSettings); //read only metadata
                    var profile = image.GetProfile("dng:thumbnail");

                    if (profile != null)
                    {
                        using (var jpgThumbnail = new MagickImage(profile.ToByteArray())) //use embedded thumbnail
                        {
                            if (IsLargerResolution(settings.ThumbnailSettings, jpgThumbnail.Width, jpgThumbnail.Height))
                            {
                                jpgThumbnail.AutoOrient(); // Correct the image orientation
                                jpgThumbnail.Thumbnail(size);
                                Directory.CreateDirectory(Path.GetDirectoryName(outFile));
                                jpgThumbnail.Write(outFile);
                                thumbnailCreated = true;
                            }
                        }
                    }
                }

                if (!thumbnailCreated)
                {
                    using (MagickImage image = new MagickImage(tmpFile, readSettings))
                    {
                        image.Thumbnail(size);
                        image.TransformColorSpace(ColorProfiles.AdobeRGB1998);
                        image.AutoLevel();
                        image.Comment = origHash;
                        Directory.CreateDirectory(Path.GetDirectoryName(outFile));
                        image.Write(outFile, MagickFormat.Jpg);
                    }
                }


                var thread = Thread.CurrentThread;
                Console.WriteLine($"[{thread.ManagedThreadId}] generated thumbnail for file {Path.GetFileName(origFile)}");

            }
            catch (MagickException ex)
            {
                attempt++;
                if (attempt < maxAttempts)
                {
                    Thread.Sleep(wait);
                    wait *= 2;
                    var thread = Thread.CurrentThread;
                    Console.WriteLine($"[{thread.ManagedThreadId}] thumbnail attempt [{attempt}] for file {Path.GetFileName(origFile)}");
                    GenerateThumbnail(settings, filePath, tmpFile, origFile, origHash, attempt, wait);
                }
                else
                {
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
                    string message = $"could not create thumbnail for {origFile}\n\t{ex.ToString().Replace(Environment.NewLine, Environment.NewLine + "\t")}";
                    lock (_lockLog)
                    {
                        Errors++;
                        _log.Add(message, LogType.ERROR, currentMethodName.Name);
                    }
                }
            }
        }

        private bool IsImage(string file)
        {
            var format = MagickFormatInfo.Create(file);
            return format.ModuleFormat == MagickFormat.Dng || (format.MimeType != null && format.MimeType.Contains("image"));
        }

        private bool IsLargerResolution(Thumbnails thumbnailSettings, uint width, uint height)
        {

            if (thumbnailSettings.Selected == ThumbnailSelect.longerSide)
            {
                return Math.Max(width, height) > thumbnailSettings.Value;
            }
            else
            {
                return Math.Min(width, height) > thumbnailSettings.Value;
            }
        }

        private bool SaveFiles(DownloadSettings settings, string fullName, string tmpFile, string origFile, byte[] origHash)
        {
            var st = new StackTrace();
            var sf = st.GetFrame(0);
            var currentMethodName = sf.GetMethod();
            // kopirovat do vsech umisteni (vcetne zalohy)
            foreach (string root in new List<string> { settings.Paths.Root, settings.Paths.Backup })
            {
                if (root == null || root == "")
                {
                    return true;
                }

                string fullPath = Path.Combine(root, fullName);

                //pokud existuje soubor se stejným názvem, ale jiným datem pořízení -> odlišení nového souboru - pridani (n) k nazvu 
                bool cont = false;

                if (File.Exists(fullPath))
                {
                    string newFileName = GetUniqueFileName(fullPath, tmpFile);
                    if (newFileName == fullPath)
                    {
                        cont = true;
                    }
                    else
                    {
                        fullPath = newFileName;
                    }
                }

                if (cont)
                {
                    string message = $"File was already downloaded [{origFile}] to [{fullPath}]";
                    lock (_lockLog)
                    {
                        _log.Add(message, LogType.INFO, currentMethodName.Name);
                    }
                    return true;
                }

                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                int i;
                for (i = 0; i < maxAttempts; i++)
                {
                    File.Copy(tmpFile, fullPath);
                    var thread = Thread.CurrentThread;
                    Console.WriteLine($"[{thread.ManagedThreadId}] copied file {Path.GetFileName(origFile)} attempt {i}");

                    if (!settings.CheckFiles || CheckFileHash(origHash, fullPath))
                    {
                        break;
                    }
                }

                // soubor se nepodařilo zkopírovat -> další soubor
                if (i == maxAttempts)
                {
                    File.Delete(fullPath);
                    string message = $"could not copy file {origFile}";
                    lock (_lockLog)
                    {
                        Errors++;
                        _log.Add(message, LogType.ERROR, currentMethodName.Name);
                    }
                    return false;
                }
            }
            return true;
        }

        private byte[] GetFileHash(Stream s)
        {
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(s);
            s.Close();
            return hash;
        }

        private bool CheckFileHash(byte[] origHash, string filePath)
        {
            return origHash.SequenceEqual(GetFileHash(File.OpenRead(filePath)));
        }

        private string GetUniqueFileName(string newFile, string tempFile, bool isThumbnail = false)
        {
            int i = 1;
            try
            {
                while (File.Exists(newFile))
                {
                    if (isThumbnail || !CheckFileHash(GetFileHash(File.OpenRead(tempFile)), newFile))
                    {
                        string ext = Path.GetExtension(newFile);
                        newFile = newFile.Replace(ext, $"({i}){ext}");
                    }
                    else { break; }
                    i++;
                }
            }
            catch { }

            return newFile;
        }
    }
}
