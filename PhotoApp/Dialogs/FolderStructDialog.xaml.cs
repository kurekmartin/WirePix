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

namespace PhotoApp.Dialogs
{
    public class FolderLevel : ObservableObject
    {
        private int _index;
        private ObservableCollection<string> _tags;

        public FolderLevel(int index, List<string> tags = null)
        {
            Index = index;
            if (tags != null)
            {
                Tags = new ObservableCollection<string>(tags);
            }
            else
            {
                Tags = new ObservableCollection<string>();
            }

        }
        public int Index
        {
            get { return _index; }
            set
            {
                _index = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<string> Tags
        {
            get { return _tags; }
            set
            {
                _tags = value;
                _tags.CollectionChanged += Tags_CollectionChanged;
                OnPropertyChanged();
            }
        }

        private void Tags_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("Tags");
        }
    }

    public partial class FolderStructDialog : UserControl, INotifyPropertyChanged
    {
        private MainWindow mainWindow;
        private ObservableCollection<FolderLevel> _folderStructure;//struktura pro uložení fotek
        private List<ButtonGroupStruct> _buttons = new List<ButtonGroupStruct>();
        private Point _buttonGridSize = new Point();
        private int _selectedFolderIndex = -1; //index vybrané složky
        private int _selectedTagIndex = -1; //index vybraneho tagu

        private static int _folderLimit = 7;

        public ObservableCollection<FolderLevel> FolderStructure
        {
            get { return _folderStructure; }
            set
            {
                _folderStructure = value;
                _folderStructure.CollectionChanged += FolderStructure_CollectionChanged;
                OnPropertyChanged();
            }
        }

        private void FolderStructure_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ObservableCollection<FolderLevel> folders = sender as ObservableCollection<FolderLevel>;
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
            int i = 0;
            foreach (FolderLevel folder in folders)
            {
                if (i++ > index)
                {
                    folder.Index += change;
                }
            }
            OnPropertyChanged("FolderStructure");
        }

        public int SelectedFolderIndex
        {
            get { return _selectedFolderIndex; }
            set
            {
                if (_selectedFolderIndex != value)
                {
                    _selectedFolderIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged("SelectedFolder");
                    if (SelectedFolder != null && SelectedFolder.Tags.Count > 0) { SelectedTagIndex = 0; }
                }
            }
        }

        public int SelectedTagIndex
        {
            get { return _selectedTagIndex; }
            set { _selectedTagIndex = value; OnPropertyChanged(); }
        }

        public FolderLevel SelectedFolder
        {
            get
            {
                if (SelectedFolderIndex > -1 && SelectedFolderIndex < FolderStructure.Count)
                {
                    return FolderStructure[SelectedFolderIndex];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                FolderStructure[SelectedFolderIndex] = value;
                OnPropertyChanged();
            }
        }

        public List<ButtonGroupStruct> Buttons
        {
            get { return _buttons; }
            set { _buttons = value; OnPropertyChanged(); }
        }

        public Point ButtonGridSize
        {
            get { return _buttonGridSize; }
            set { _buttonGridSize = value; OnPropertyChanged(); }
        }

        private void SelectedFolder_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("SelectedFolder");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public FolderStructDialog(MainWindow window, List<ButtonGroupStruct> buttons, List<List<string>> initStructure = null)
        {
            FolderStructure = new ObservableCollection<FolderLevel>();
            DataContext = this;
            InitializeComponent();
            mainWindow = window;

            ButtonGridSize = new Point(buttons.Max(x => x.gridPosition.X) + 1,
                                       buttons.Max(x => x.gridPosition.Y) + 1);
            Buttons = buttons;

            int i = 0;
            initStructure.ForEach(x => FolderStructure.Add(new FolderLevel(i++, x)));
            if (FolderStructure.Count == 0)
            {
                //vytvoreni korene treeView
                NewFolderLevel();
            }
            else
            {
                SelectedFolderIndex = 0;
                if (SelectedFolder.Tags.Count > 0) { SelectedTagIndex = 0; }
            }
            ShowControls();
        }


        //přidání tagu po kliknutí na tlačítko
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            tbError.Text = ""; //reset chybové hlášky
            string tagCode = ((Button)sender).Tag.ToString();

            if (SelectedFolderIndex == -1) { SelectedFolderIndex = FolderStructure.Count() - 1; }

            if (ValidCustomText(tbCustomText))
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
                else if (TagAdd(tagCode, FolderStructure[SelectedFolderIndex].Tags.ToList(), tbError))
                {
                    FolderStructure[SelectedFolderIndex].Tags.Insert(SelectedTagIndex + 1, tagCode);
                    SelectedTagIndex++;
                    ShowControls();
                }
            }
            else
            {
                ShowCustomTextError(tbCustomText, tbControlsError);
            }


        }

        //vytvoření nové složky
        private void NewFolderLevel()
        {
            FolderStructure.Insert(SelectedFolderIndex + 1, new FolderLevel(SelectedFolderIndex + 1));
            SelectedFolderIndex++;
            SelectedTagIndex = -1;
            btnDeleteFolder.Visibility = Visibility.Collapsed;
        }


        //kontrola textBoxu - povolené jen písmena - _
        private void tbCustomText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //Regex regex = new Regex(@"^[a-zA-Z0-9\-_]*$");
            //e.Handled = !regex.IsMatch(e.Text);

            e.Handled = e.Text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
        }


        //změna vybraneho tagu
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0 && ((ComboBox)sender).SelectedIndex > -1)
            {
                TagStruct tag = (TagStruct)((ComboBox)sender).SelectedItem;
                int oldIndex = SelectedTagIndex;
                FolderStructure[SelectedFolderIndex].Tags[SelectedTagIndex] = tag.Code;
                SelectedTagIndex = oldIndex;
            }
        }

        //vyber tagu v nazvu souboru
        private void TagSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbControlsError.Visibility = Visibility.Hidden;
            if (e.AddedItems.Count > 0 && e.RemovedItems.Count > 0 && Tags.GetTag(code: e.RemovedItems[0].ToString()).Code == Properties.TagCodes.CustomText) //kontrola parametru CustomText
            {
                string tag = FolderStructure[SelectedFolderIndex].Tags.First(x => x == e.RemovedItems[0].ToString());
                string text = Tags.GetParameter(tag);
                if (!Tags.IsValidFileName(text))
                {
                    SelectedTagIndex = FolderStructure[SelectedFolderIndex].Tags.IndexOf(tag);
                    ShowCustomTextError(tbCustomText, tbControlsError);
                }
            }
            ShowControls();
        }

        //smazani vybraneho tagu
        private void btnDeleteTag_Click(object sender, RoutedEventArgs e)
        {
            int oldIndex = SelectedTagIndex;

            if (FolderStructure[SelectedFolderIndex].Tags.Count > 0)
            {
                FolderStructure[SelectedFolderIndex].Tags.RemoveAt(SelectedTagIndex);
                if (oldIndex >= FolderStructure[SelectedFolderIndex].Tags.Count)
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
            FolderStructure.RemoveAt(SelectedFolderIndex);
            if (FolderStructure.Count == 0)
            {
                NewFolderLevel();
            }
        }

        //změna CustomTextu -> update tagu
        private void tbCustomText_TextChanged(object sender, TextChangedEventArgs e)
        {
            int oldIndex = SelectedTagIndex;
            string tag = FolderStructure[SelectedFolderIndex].Tags[SelectedTagIndex];
            tag = Tags.RemoveParameter(tag);
            tag += $"({tbCustomText.Text})";

            FolderStructure[SelectedFolderIndex].Tags[SelectedTagIndex] = tag;
            SelectedTagIndex = oldIndex;
            tbCustomText.Focus();

            if (tbCustomText.Text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                ShowCustomTextError(tbCustomText, tbControlsError);
            }
            else { tbError.Text = ""; }
        }

        private void ShowControls()
        {
            cbGroupSelect.Visibility = tbCustomText.Visibility = btnDeleteTag.Visibility = Visibility.Collapsed;

            if (SelectedFolderIndex > -1 && FolderStructure.Count() > 0 && FolderStructure[SelectedFolderIndex].Tags.Count() > 0)
            {
                btnDeleteFolder.Visibility = Visibility.Visible;

                if (SelectedTagIndex >= 0)
                {
                    btnDeleteTag.Visibility = Visibility.Visible;

                    string tagText = FolderStructure[SelectedFolderIndex].Tags[SelectedTagIndex];
                    TagStruct tag = Tags.GetTag(code: tagText);

                    if (tag.Group != string.Empty)
                    {
                        List<TagStruct> tagGroup = Tags.GetTagGroup(tag.Group);

                        cbGroupSelect.ItemsSource = tagGroup;
                        cbGroupSelect.SelectedIndex = tagGroup.IndexOf(tag);
                        cbGroupSelect.Visibility = Visibility.Visible;
                    }
                    else if (tag.Code == Properties.TagCodes.CustomText)
                    {
                        tbCustomText.Visibility = Visibility.Visible;
                        tbCustomText.Text = Tags.GetParameter(tagText);
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
                OnPropertyChanged("SelectedFolder");
                ShowControls();
                if (FolderStructure.Count > 0 && SelectedFolderIndex != FolderStructure.Count() - 1 && FolderStructure.Last().Tags.Count == 0)
                {
                    FolderStructure.Remove(FolderStructure.Last());
                }
            }
            else if (e.RemovedItems.Count > 0 && !Tags.IsValidFileName(tbCustomText.Text))
            {
                SelectedFolderIndex = ((FolderLevel)e.RemovedItems[0]).Index;
                ShowCustomTextError(tbCustomText, tbControlsError);
            }

        }

        //ukončení formuláře
        private void btnDone_Click(object sender, RoutedEventArgs e)
        {
            if (ValidCustomText(tbCustomText))
            {
                List<List<string>> result = new List<List<string>>();
                FolderStructure.ToList().ForEach(x => result.Add(x.Tags.ToList()));
                mainWindow.DialogClose(this, result, MainWindow.RESULT_OK);
            }
            else
            {
                ShowCustomTextError(tbCustomText, tbControlsError);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.DialogClose(this, resultCode: MainWindow.RESULT_CANCEL);
        }
    }
}
