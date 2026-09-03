namespace PhotoApp;

public readonly record struct DeviceStorageSpace(
    double TotalGigabytes,
    double AvailableGigabytes,
    double UsedGigabytes);
