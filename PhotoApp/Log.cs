using System;
using System.IO;
using System.Windows;

namespace PhotoApp;

public enum LogType
{
    INFO,
    WARNING,
    ERROR
}

internal class Log
{
    private bool _running { get; set; } = false;
    private string _currentLog;
    private static string _timeFormat = "yyyy-MM-dd HH:mm:ss,fff";
    private static string _fileNameFormat = "yyyy-MM-dd HH-mm-ss";
    private string _logFolder = Application.Current.Resources[key: Properties.Keys.LogsFolder].ToString();

    public Log()
    {
        Directory.CreateDirectory(path: _logFolder);
    }

    public void Start()
    {
        var fileName = DateTime.Now.ToString(format: _fileNameFormat);
        _currentLog = Path.Combine(path1: _logFolder, path2: fileName + ".log");
        _running = true;
        //File.CreateText(_currentLog);
    }

    public void Add(string message, LogType type, string functionName = "")
    {
        var time = DateTime.Now.ToString(format: _timeFormat);
        File.AppendAllText(path: _currentLog, contents: $"[{time}][{type}]{functionName} - {message}\n");
    }

    public void Stop()
    {
        var file = new FileInfo(fileName: _currentLog);
        _running = false;
    }

    public bool Running
    {
        get => _running;

        private set => _running = value;
    }
}