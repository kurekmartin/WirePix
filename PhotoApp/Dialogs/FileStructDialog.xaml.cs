using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using static PhotoApp.Dialogs.BaseStructDialog;

namespace PhotoApp.Dialogs;

public partial class FileStructDialog : UserControl, INotifyPropertyChanged
{
    private MainWindow mainWindow;
    private ObservableCollection<string> _fileStructure = new(); //struktura souboru
    private List<ButtonGroupStruct> _buttons = new();
    private Point _buttonGridSize = new();
    private int _selectedIndex = -1;

    public ObservableCollection<string> FileStructure
    {
        get => _fileStructure;
        set
        {
            _fileStructure = value;
            OnPropertyChanged();
        }
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            _selectedIndex = value;
            OnPropertyChanged();
        }
    }

    public List<ButtonGroupStruct> Buttons
    {
        get => _buttons;
        set
        {
            _buttons = value;
            OnPropertyChanged();
        }
    }

    public Point ButtonGridSize
    {
        get => _buttonGridSize;
        set
        {
            _buttonGridSize = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(sender: this, e: new PropertyChangedEventArgs(propertyName: propertyName));
    }

    public FileStructDialog(MainWindow window, List<ButtonGroupStruct> buttons, List<string> initStructure = null)
    {
        DataContext = this;
        InitializeComponent();
        mainWindow = window;

        ButtonGridSize = new Point(x: buttons.Max(selector: x => x.gridPosition.X) + 1,
            y: buttons.Max(selector: x => x.gridPosition.Y) + 1);
        Buttons = buttons;

        FileStructure = new ObservableCollection<string>(list: initStructure);
        if (FileStructure.Count > 0)
        {
            SelectedIndex = 0;
        }

        ShowControls();
    }


    //přidání tagu po kliknutí na tlačítko
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        tbError.Text = ""; //reset chybové hlášky
        var insertValue = ((Button)sender).Tag.ToString();

        if (TagAdd(tagCode: insertValue, tags: FileStructure.ToList(), error: tbError))
        {
            FileStructure.Insert(index: SelectedIndex + 1, item: insertValue);
            SelectedIndex++;
        }

        if (FileStructure.Count() > 0)
        {
            ShowControls();
        }
    }

    private void ShowControls()
    {
        cbGroupSelect.Visibility = tbCustomText.Visibility = Visibility.Collapsed;

        if (FileStructure.Count() > 0)
        {
            tbFileExt.Visibility = Visibility.Visible;

            if (SelectedIndex >= 0)
            {
                btnDeleteTag.Visibility = Visibility.Visible;

                string tagText = FileStructure[index: SelectedIndex];
                TagStruct tag = Tags.GetTag(code: tagText);

                if (tag.Group != string.Empty)
                {
                    List<TagStruct> tagGroup = Tags.GetTagGroup(code: tag.Group);

                    cbGroupSelect.ItemsSource = tagGroup;
                    cbGroupSelect.SelectedIndex = tagGroup.IndexOf(item: tag);
                    cbGroupSelect.Visibility = Visibility.Visible;
                }
                else if (tag.Code == Properties.TagCodes.CustomText)
                {
                    tbCustomText.Visibility = Visibility.Visible;
                    tbCustomText.Text = Tags.GetParameter(visibleText: tagText);
                }
            }
        }
        else
        {
            tbFileExt.Visibility = Visibility.Hidden;
            btnDeleteTag.Visibility = Visibility.Hidden;
        }
    }


    //vyber tagu v nazvu souboru
    private void TagSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        tbControlsError.Visibility = Visibility.Hidden;
        if (e.AddedItems.Count > 0 && e.RemovedItems.Count > 0 && Tags.GetTag(code: e.RemovedItems[index: 0].ToString()).Code == Properties.TagCodes.CustomText) //kontrola parametru CustomText
        {
            string tag = FileStructure.First(predicate: x => x == e.RemovedItems[index: 0].ToString());
            string text = Tags.GetParameter(visibleText: tag);
            if (!Tags.IsValidFileName(text: text))
            {
                SelectedIndex = FileStructure.IndexOf(item: tag);
                ShowCustomTextError(customText: tbCustomText, errorBlock: tbControlsError);
            }
        }

        ShowControls();
    }

    //kontrola textBoxu - povolené jen písmena - _
    private void tbCustomText_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        //Regex regex = new Regex(@"^[a-zA-Z0-9\-_]*$");
        //e.Handled = !regex.IsMatch(e.Text);

        e.Handled = e.Text.IndexOfAny(anyOf: Path.GetInvalidFileNameChars()) >= 0;
    }


    //změna vybraneho tagu
    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.RemovedItems.Count > 0 && ((ComboBox)sender).SelectedIndex > -1)
        {
            var tag = (TagStruct)((ComboBox)sender).SelectedItem;
            int oldIndex = SelectedIndex;
            FileStructure[index: SelectedIndex] = tag.Code;
            SelectedIndex = oldIndex;
        }
    }

    //smazani vybraneho tagu
    private void btnDeleteTag_Click(object sender, RoutedEventArgs e)
    {
        int oldIndex = SelectedIndex;

        if (FileStructure.Count > 0 && SelectedIndex > -1)
        {
            FileStructure.RemoveAt(index: SelectedIndex);
            if (oldIndex >= FileStructure.Count)
            {
                SelectedIndex = oldIndex - 1;
            }
            else
            {
                SelectedIndex = oldIndex;
            }
        }

        ShowControls();
    }


    //změna CustomTextu -> update tagu
    private void tbCustomText_TextChanged(object sender, TextChangedEventArgs e)
    {
        int oldIndex = SelectedIndex;
        string tag = FileStructure[index: SelectedIndex];

        tag = Tags.RemoveParameter(tag: tag);
        tag += $"({tbCustomText.Text})";

        FileStructure[index: SelectedIndex] = tag;
        SelectedIndex = oldIndex;
        tbCustomText.Focus();

        if (tbCustomText.Text.IndexOfAny(anyOf: Path.GetInvalidFileNameChars()) >= 0)
        {
            ShowCustomTextError(customText: tbCustomText, errorBlock: tbControlsError);
        }
        else
        {
            tbError.Text = "";
        }
    }

    //ukončení formuláře
    private void btnDone_Click(object sender, RoutedEventArgs e)
    {
        if (ValidCustomText(textBox: tbCustomText))
        {
            mainWindow.DialogClose(sender: this, result: FileStructure.ToList(), resultCode: MainWindow.RESULT_OK);
        }
        else
        {
            ShowCustomTextError(customText: tbCustomText, errorBlock: tbControlsError);
        }
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        mainWindow.DialogClose(sender: this, resultCode: MainWindow.RESULT_CANCEL);
    }
}