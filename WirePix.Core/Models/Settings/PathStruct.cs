using System.Collections.Generic;

namespace WirePix.Core.Models.Settings;

public sealed class PathStruct : ObservableObject
{
    private string _root = string.Empty, _backup = string.Empty, _thumbnail = string.Empty;
    private List<List<string>> _folderTags = [];
    private List<string> _fileTags = [];

    public string Root
    {
        get => _root;
        set
        {
            if (value == _root)
            {
                return;
            }

            _root = value;
            OnPropertyChanged();
        }
    }

    public List<List<string>> FolderTags
    {
        get => _folderTags;
        set
        {
            if (value == _folderTags)
            {
                return;
            }

            _folderTags = value;
            OnPropertyChanged();
        }
    }

    public List<string> FileTags
    {
        get => _fileTags;
        set
        {
            if (value == _fileTags)
            {
                return;
            }

            _fileTags = value;
            OnPropertyChanged();
        }
    }

    public string Backup
    {
        get => _backup;
        set
        {
            if (value == _backup)
            {
                return;
            }

            _backup = value;
            OnPropertyChanged();
        }
    }

    public string Thumbnail
    {
        get => _thumbnail;
        set
        {
            if (value == _thumbnail)
            {
                return;
            }

            _thumbnail = value;
            OnPropertyChanged();
        }
    }
}