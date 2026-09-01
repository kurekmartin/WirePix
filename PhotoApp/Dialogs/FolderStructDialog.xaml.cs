using WirePix.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static PhotoApp.Dialogs.BaseStructDialog;

namespace PhotoApp.Dialogs;

public class FolderLevel : ObservableObject
{
    private int _index;
    private ObservableCollection<string> _tags;

    public FolderLevel(int index, List<string> tags = null)
    {
        Index = index;
        if (tags != null)
        {
            Tags = new ObservableCollection<string>(list: tags);
        }
        else
        {
            Tags = new ObservableCollection<string>();
        }
    }

    public int Index
    {
        get => _index;
        set
        {
            _index = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> Tags
    {
        get => _tags;
        set
        {
            _tags = value;
            _tags.CollectionChanged += Tags_CollectionChanged;
            OnPropertyChanged();
        }
    }

    private void Tags_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(propertyName: "Tags");
    }
}

public partial class FolderStructDialog : UserControl, INotifyPropertyChanged
{
    private MainWindow mainWindow;
    private ObservableCollection<FolderLevel> _folderStructure; //struktura pro uložení fotek
    private List<ButtonGroupStruct> _buttons = new();
    private Point _buttonGridSize = new();
    private int _selectedFolderIndex = -1; //index vybrané složky
    private int _selectedTagIndex = -1; //index vybraneho tagu

    private static int _folderLimit = 7;

    public ObservableCollection<FolderLevel> FolderStructure
    {
        get => _folderStructure;
        set
        {
            _folderStructure = value;
            _folderStructure.CollectionChanged += FolderStructure_CollectionChanged;
            OnPropertyChanged();
        }
    }

    private void FolderStructure_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        var folders = sender as ObservableCollection<FolderLevel>;
        int index = e.OldStartingIndex == -1 ? e.NewStartingIndex : e.OldStartingIndex;
        short change = 0;
        if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            change = -1;
        }
        else if (e.Action == NotifyCollectionChangedAction.Add)
        {
            change = 1;
        }

        var i = 0;
        foreach (FolderLevel folder in folders)
        {
            if (i++ > index)
            {
                folder.Index += change;
            }
        }

        OnPropertyChanged(propertyName: "FolderStructure");
    }

    public int SelectedFolderIndex
    {
        get => _selectedFolderIndex;
        set
        {
            if (_selectedFolderIndex != value)
            {
                _selectedFolderIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(propertyName: "SelectedFolder");
                if (SelectedFolder != null && SelectedFolder.Tags.Count > 0)
                {
                    SelectedTagIndex = 0;
                }
            }
        }
    }

    public int SelectedTagIndex
    {
        get => _selectedTagIndex;
        set
        {
            _selectedTagIndex = value;
            OnPropertyChanged();
        }
    }

    public FolderLevel SelectedFolder
    {
        get
        {
            if (SelectedFolderIndex > -1 && SelectedFolderIndex < FolderStructure.Count)
            {
                return FolderStructure[index: SelectedFolderIndex];
            }
            else
            {
                return null;
            }
        }
        set
        {
            FolderStructure[index: SelectedFolderIndex] = value;
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

    private void SelectedFolder_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(propertyName: "SelectedFolder");
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(sender: this, e: new PropertyChangedEventArgs(propertyName: propertyName));
    }

    public FolderStructDialog(MainWindow window, List<ButtonGroupStruct> buttons, List<List<string>> initStructure = null)
    {
        FolderStructure = new ObservableCollection<FolderLevel>();
        DataContext = this;
        InitializeComponent();
        mainWindow = window;

        ButtonGridSize = new Point(x: buttons.Max(selector: x => x.gridPosition.X) + 1,
            y: buttons.Max(selector: x => x.gridPosition.Y) + 1);
        Buttons = buttons;

        var i = 0;
        initStructure.ForEach(action: x => FolderStructure.Add(item: new FolderLevel(index: i++, tags: x)));
        if (FolderStructure.Count == 0)
        {
            //vytvoreni korene treeView
            NewFolderLevel();
        }
        else
        {
            SelectedFolderIndex = 0;
            if (SelectedFolder.Tags.Count > 0)
            {
                SelectedTagIndex = 0;
            }
        }

        ShowControls();
    }


    //přidání tagu po kliknutí na tlačítko
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        tbError.Text = ""; //reset chybové hlášky
        var tagCode = ((Button)sender).Tag.ToString();

        if (SelectedFolderIndex == -1)
        {
            SelectedFolderIndex = FolderStructure.Count() - 1;
        }

        if (ValidCustomText(textBox: tbCustomText))
        {
            if (tagCode == Properties.TagCodes.NewFolder) //přidání nové složky
            {
                if (SelectedFolder.Tags.Count() > 0)
                {
                    if (FolderStructure.Count() >= _folderLimit)
                    {
                        tbError.Text = $"Můžete použít maximálně {_folderLimit} složek";
                    }
                    else
                    {
                        NewFolderLevel();
                    }
                }
            }
            else if (TagAdd(tagCode: tagCode, tags: FolderStructure[index: SelectedFolderIndex].Tags.ToList(), error: tbError))
            {
                FolderStructure[index: SelectedFolderIndex].Tags.Insert(index: SelectedTagIndex + 1, item: tagCode);
                SelectedTagIndex++;
                ShowControls();
            }
        }
        else
        {
            ShowCustomTextError(customText: tbCustomText, errorBlock: tbControlsError);
        }
    }

    //vytvoření nové složky
    private void NewFolderLevel()
    {
        FolderStructure.Insert(index: SelectedFolderIndex + 1, item: new FolderLevel(index: SelectedFolderIndex + 1));
        SelectedFolderIndex++;
        SelectedTagIndex = -1;
        btnDeleteFolder.Visibility = Visibility.Collapsed;
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
            int oldIndex = SelectedTagIndex;
            FolderStructure[index: SelectedFolderIndex].Tags[index: SelectedTagIndex] = tag.Code;
            SelectedTagIndex = oldIndex;
        }
    }

    //vyber tagu v nazvu souboru
    private void TagSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        tbControlsError.Visibility = Visibility.Hidden;
        if (e.AddedItems.Count > 0 && e.RemovedItems.Count > 0 && Tags.GetTag(code: e.RemovedItems[index: 0].ToString()).Code == Properties.TagCodes.CustomText) //kontrola parametru CustomText
        {
            string tag = FolderStructure[index: SelectedFolderIndex].Tags.First(predicate: x => x == e.RemovedItems[index: 0].ToString());
            string text = Tags.GetParameter(visibleText: tag);
            if (!Tags.IsValidFileName(text: text))
            {
                SelectedTagIndex = FolderStructure[index: SelectedFolderIndex].Tags.IndexOf(item: tag);
                ShowCustomTextError(customText: tbCustomText, errorBlock: tbControlsError);
            }
        }

        ShowControls();
    }

    //smazani vybraneho tagu
    private void btnDeleteTag_Click(object sender, RoutedEventArgs e)
    {
        int oldIndex = SelectedTagIndex;

        if (FolderStructure[index: SelectedFolderIndex].Tags.Count > 0)
        {
            FolderStructure[index: SelectedFolderIndex].Tags.RemoveAt(index: SelectedTagIndex);
            if (oldIndex >= FolderStructure[index: SelectedFolderIndex].Tags.Count)
            {
                SelectedTagIndex = oldIndex - 1;
            }
            else
            {
                SelectedTagIndex = oldIndex;
            }
        }

        ShowControls();
    }

    //smazani vybrane slozky
    private void btnDeleteFolder_Click(object sender, RoutedEventArgs e)
    {
        DeleteSelectedFolder();
    }

    private void DeleteSelectedFolder()
    {
        FolderStructure.RemoveAt(index: SelectedFolderIndex);
        if (FolderStructure.Count == 0)
        {
            NewFolderLevel();
        }
    }

    //změna CustomTextu -> update tagu
    private void tbCustomText_TextChanged(object sender, TextChangedEventArgs e)
    {
        int oldIndex = SelectedTagIndex;
        string tag = FolderStructure[index: SelectedFolderIndex].Tags[index: SelectedTagIndex];
        tag = Tags.RemoveParameter(tag: tag);
        tag += $"({tbCustomText.Text})";

        FolderStructure[index: SelectedFolderIndex].Tags[index: SelectedTagIndex] = tag;
        SelectedTagIndex = oldIndex;
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

    private void ShowControls()
    {
        cbGroupSelect.Visibility = tbCustomText.Visibility = btnDeleteTag.Visibility = Visibility.Collapsed;

        if (SelectedFolderIndex > -1 && FolderStructure.Count() > 0 && FolderStructure[index: SelectedFolderIndex].Tags.Count() > 0)
        {
            btnDeleteFolder.Visibility = Visibility.Visible;

            if (SelectedTagIndex >= 0)
            {
                btnDeleteTag.Visibility = Visibility.Visible;

                string tagText = FolderStructure[index: SelectedFolderIndex].Tags[index: SelectedTagIndex];
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
                    tbCustomText.Focus();
                }
            }
            else
            {
                btnDeleteTag.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            btnDeleteFolder.Visibility = Visibility.Hidden;
        }
    }

    private void FolderSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (tbCustomText.Visibility != Visibility.Visible)
        {
            OnPropertyChanged(propertyName: "SelectedFolder");
            ShowControls();
            if (FolderStructure.Count > 0 && SelectedFolderIndex != FolderStructure.Count() - 1 && FolderStructure.Last().Tags.Count == 0)
            {
                FolderStructure.Remove(item: FolderStructure.Last());
            }
        }
        else if (e.RemovedItems.Count > 0 && !Tags.IsValidFileName(text: tbCustomText.Text))
        {
            SelectedFolderIndex = ((FolderLevel)e.RemovedItems[index: 0]).Index;
            ShowCustomTextError(customText: tbCustomText, errorBlock: tbControlsError);
        }
    }

    //ukončení formuláře
    private void btnDone_Click(object sender, RoutedEventArgs e)
    {
        if (ValidCustomText(textBox: tbCustomText))
        {
            var result = new List<List<string>>();
            FolderStructure.ToList().ForEach(action: x => result.Add(item: x.Tags.ToList()));
            mainWindow.DialogClose(sender: this, result: result, resultCode: MainWindow.RESULT_OK);
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