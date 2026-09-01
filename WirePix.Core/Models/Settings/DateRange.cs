using System;

namespace WirePix.Core.Models.Settings;

public sealed class DateRange : ObservableObject
{
    public DateRange() => Start = End = DateTime.Now.Date;

    public DateTime Start
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

    public DateTime End
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

    public static bool operator ==(DateRange a, DateRange b) => ReferenceEquals(a, b) || (a != null && b != null && a.Start == b.Start && a.End == b.End);
    public static bool operator !=(DateRange a, DateRange b) => !(a == b);
    public override bool Equals(object obj) => obj is DateRange other && this == other;
    public override int GetHashCode() => HashCode.Combine(Start, End);
}