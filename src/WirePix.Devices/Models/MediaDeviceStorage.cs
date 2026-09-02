namespace WirePix.Devices.Models;

/// <summary>
/// Represents aggregate device storage without exposing drives, volumes, or paths.
/// </summary>
public sealed record MediaDeviceStorage
{
    /// <summary>
    /// Creates an aggregate storage snapshot.
    /// </summary>
    /// <param name="totalBytes">The total capacity in bytes.</param>
    /// <param name="availableBytes">The available capacity in bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A value is negative, or available capacity exceeds total capacity.
    /// </exception>
    public MediaDeviceStorage(long totalBytes, long availableBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(availableBytes);
        if (availableBytes > totalBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(availableBytes),
                availableBytes,
                "Available storage cannot exceed total storage.");
        }

        TotalBytes = totalBytes;
        AvailableBytes = availableBytes;
    }

    /// <summary>
    /// Gets the total capacity in bytes.
    /// </summary>
    public long TotalBytes { get; }

    /// <summary>
    /// Gets the available capacity in bytes.
    /// </summary>
    public long AvailableBytes { get; }

    /// <summary>
    /// Gets the used capacity in bytes.
    /// </summary>
    public long UsedBytes => TotalBytes - AvailableBytes;
}
