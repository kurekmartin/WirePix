using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PhotoApp.Properties;

namespace PhotoApp.Dialogs
{

    /// <summary>
    /// Interakční logika pro SaveDialog.xaml
    /// </summary>
    public partial class SaveDialog : UserControl
    {
        private SaveOptions saveResult;
        private MainWindow mainWindow;
        public SaveDialog(MainWindow window, SaveOptions options = null)
        {
            InitializeComponent();
            if (options == null)
            {
                saveResult = new SaveOptions();
            }
            else
            {
                saveResult = options;
            }
            DataContext = saveResult;
            mainWindow = window;
        }

        private bool ValidFileName(string filename)
        {
            char[] invalidFileChars = Path.GetInvalidFileNameChars();
            return filename.Length > 0 && filename.IndexOfAny(invalidFileChars.ToArray()) == -1;
        }

        private void tbFileName_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!ValidFileName(tb.Text))
            {
                tbError.Text = Properties.Resources.InvalidFilename;
            }
            else if (File.Exists(Path.Combine(Application.Current.Resources[Keys.ProfilesFolder].ToString(), $"{tb.Text}.xml")))
            {
                tbError.Text = Properties.Resources.FileExists_Overwrite;
            }
            else
            {
                tbError.Text = string.Empty;
            }
        }

        private void cbStorage_Checked(object sender, RoutedEventArgs e)
        {
            saveResult.Root = saveResult.FileStruct = saveResult.FolderStruct = true;
        }

        private void cbStorage_Unchecked(object sender, RoutedEventArgs e)
        {
            saveResult.Root = saveResult.FileStruct = saveResult.FolderStruct = false;
        }


        private void cbStorageItem_Changed(object sender, RoutedEventArgs e)
        {
            if (saveResult.Root && saveResult.FileStruct && saveResult.FolderStruct)
            {
                cbStorage.IsChecked = true;
            }
            else if (!saveResult.Root && !saveResult.FileStruct && !saveResult.FolderStruct)
            {
                cbStorage.IsChecked = false;
            }
            else
            {
                cbStorage.IsChecked = null;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (ValidFileName(saveResult.FileName))
            {
                mainWindow.DialogClose(this, saveResult, MainWindow.RESULT_OK);
            }
        }
    }
}
