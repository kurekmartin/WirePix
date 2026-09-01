using System;
using System.IO;

namespace WirePix.Core.Logging;

public sealed class FileLogger
{
    private readonly string _folder;
    private string _file;

    public FileLogger(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(path: folder);
    }

    public bool Running => _file != null;

    public void Start()
    {
        Directory.CreateDirectory(path: _folder);
        _file = Path.Combine(path1: _folder, path2: DateTime.Now.ToString(format: "yyyy-MM-dd HH-mm-ss") + ".log");
    }

    public void Add(string message, LogType type, string function = "")
    {
        if (_file == null)
        {
            Start();
        }

        if (_file != null)
        {
            File.AppendAllText(path: _file, contents: $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff}][{type}]{function} - {message}{Environment.NewLine}");
        }
    }

    public void Stop()
    {
        _file = null;
    }
}