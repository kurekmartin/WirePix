using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PhotoApp;

/// <summary>
/// Interakční logika pro ProgressDialog.xaml
/// </summary>
public partial class ProgressDialog : UserControl, INotifyPropertyChanged
{
    private MainWindow mainWindow;
    public TimeSpan timeRemain { get; private set; } = new();
    private TimeSpan lastReportTime = new();
    private bool countdownRunning = false;
    private readonly object lockTime = new();

    public ProgressDialog(MainWindow window)
    {
        InitializeComponent();
        mainWindow = window;
    }

    public void SetCurrentTask(string taskName)
    {
        lblCurrentTask.Text = taskName;
    }

    public void SetCurrentProgress(string progressMessage, int progress, TimeSpan time = new())
    {
        pbProgress.IsIndeterminate = false;
        lblProgress.Text = progressMessage;
        pbProgress.Value = progress;
        lock (lockTime)
        {
            if (lastReportTime != time)
            {
                timeRemain = lastReportTime = time;
            }

            OnPropertyChanged(propertyName: "timeRemain");
        }

        lblTime.Visibility = Visibility.Visible;

        if (timeRemain.Ticks > 0 && !countdownRunning)
        {
            Countdown();
        }
    }

    public void SetProgressMessage(string message)
    {
        lblProgress.Text = message;
    }

    public void SetCurrentDir(string dirName)
    {
        lblCurrentFile.Text = dirName;
    }

    public void SetIndeterminateProgress()
    {
        pbProgress.IsIndeterminate = true;
        lblTime.Visibility = Visibility.Collapsed;
    }

    private async void Countdown()
    {
        var sec = new TimeSpan(hours: 0, minutes: 0, seconds: 1);
        countdownRunning = true;
        Task countdown = Task.Run(action: () =>
        {
            while (timeRemain.TotalSeconds > 0)
            {
                System.Threading.Thread.Sleep(millisecondsTimeout: 1000);
                lock (lockTime)
                {
                    timeRemain = timeRemain.Subtract(ts: sec);
                    OnPropertyChanged(propertyName: "timeRemain");
                }
            }
        });
        await countdown;
        countdownRunning = false;
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        mainWindow.worker_Cancel();
        btnCancel.Content = new CircularProgress();
        btnCancel.IsEnabled = false;
        lblTime.Visibility = Visibility.Hidden;
        lock (lockTime)
        {
            timeRemain = new TimeSpan(ticks: 0);
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        btnCancel.IsEnabled = true;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(sender: this, e: new PropertyChangedEventArgs(propertyName: propertyName));
    }
}