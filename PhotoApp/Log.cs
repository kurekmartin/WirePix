using System;
using System.IO;
using System.Windows;

namespace PhotoApp;

public enum LogType
{
    INFO, WARNING, ERROR
}
class Log
{
    private bool _running { get; set; } = false;
    private string _currentLog;
    private static string _timeFormat = "yyyy-MM-dd HH:mm:ss,fff";
    private static string _fileNameFormat = "yyyy-MM-dd HH-mm-ss";
    private string _logFolder = Application.Current.Resources[Properties.Keys.LogsFolder].ToString();
    public Log()
    {
        Directory.CreateDirectory(_logFolder);
    }
    public void Start()
    {
        string fileName = DateTime.Now.ToString(_fileNameFormat);
        _currentLog = Path.Combine(_logFolder, fileName + ".log");
        _running = true;
        //File.CreateText(_currentLog);
    }

    public void Add(string message, LogType type, string functionName = "")
    {
        string time = DateTime.Now.ToString(_timeFormat);
        File.AppendAllText(_currentLog, $"[{time}][{type}]{functionName} - {message}\n");
    }

    public void Stop()
    {
        var file = new FileInfo(_currentLog);
        _running = false;
    }

    public bool Running
    {
        get => _running;

        private set => _running = value;
    }

}