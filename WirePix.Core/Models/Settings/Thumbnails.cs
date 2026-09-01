namespace WirePix.Core.Models.Settings;

public sealed class Thumbnails : ObservableObject
{
    public ThumbnailSelect Selected
    {
        get;
        set
        {
            if (value == field)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    }

    public int Value
    {
        get;
        set
        {
            if (value == field)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    }
}