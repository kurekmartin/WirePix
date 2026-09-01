using System;
using System.IO;

namespace WirePix.Core.Storage
{
    public enum LogType
    {
        Info,
        Warning,
        Error
    }

    public sealed class FileLogger
    {
        private readonly string _folder;
        private string _file;

        public FileLogger(string folder)
        {
            _folder = folder;
            Directory.CreateDirectory(folder);
        }

        public bool Running => _file != null;

        public void Start()
        {
            Directory.CreateDirectory(_folder);
            _file = Path.Combine(_folder, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".log");
        }

        public void Add(string message, LogType type, string function = "")
        {
            if (_file == null)
            {
                Start();
            }

            if (_file != null)
            {
                File.AppendAllText(_file, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff}][{type}]{function} - {message}{Environment.NewLine}");
            }
        }

        public void Stop()
        {
            _file = null;
        }
    }
}