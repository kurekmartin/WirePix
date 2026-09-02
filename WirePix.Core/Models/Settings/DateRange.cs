using System;

namespace WirePix.Core.Models.Settings;

public sealed class DateRange : ObservableObject
{
    public DateRange()
    {
        Start = End = DateTime.Now.Date;
    }

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

    public static bool operator ==(DateRange a, DateRange b)
    {
        if (ReferenceEquals(objA: a, objB: b))
        {
            return true;
        }

        return a is not null && b is not null && a.Start == b.Start && a.End == b.End;
    }

    public static bool operator !=(DateRange a, DateRange b)
    {
        return !(a == b);
    }

    public override bool Equals(object obj)
    {
        return obj is DateRange other && this == other;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(value1: Start, value2: End);
    }
}
