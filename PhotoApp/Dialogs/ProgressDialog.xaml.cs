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
    public TimeSpan timeRemain { get; private set; } = new TimeSpan();
    private TimeSpan lastReportTime = new TimeSpan();
    private bool countdownRunning = false;
    private readonly object lockTime = new object();

    public ProgressDialog(MainWindow window)
    {
        InitializeComponent();
        mainWindow = window;
    }

    public void SetCurrentTask(string taskName)
    {
        lblCurrentTask.Text = taskName;
    }

    public void SetCurrentProgress(string progressMessage, int progress, TimeSpan time = new TimeSpan())
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
            OnPropertyChanged("timeRemain");
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
        TimeSpan sec = new TimeSpan(0, 0, 1);
        countdownRunning = true;
        var countdown = Task.Run(() =>
        {
            while (timeRemain.TotalSeconds > 0)
            {
                System.Threading.Thread.Sleep(1000);
                lock (lockTime)
                {
                    timeRemain = timeRemain.Subtract(sec);
                    OnPropertyChanged("timeRemain");
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
            timeRemain = new TimeSpan(0);
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        btnCancel.IsEnabled = true;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}