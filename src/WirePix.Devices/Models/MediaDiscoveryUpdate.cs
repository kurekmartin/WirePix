namespace WirePix.Devices.Models;

/// <summary>
/// Reports the state of media discovery and any recoverable directory failure.
/// </summary>
/// <param name="CurrentPath">The backend-native directory currently being inspected.</param>
/// <param name="FilesFound">The number of distinct media items discovered so far.</param>
/// <param name="Error">
/// The error that prevented the current directory from being inspected, or
/// <see langword="null"/> when discovery is proceeding normally.
/// </param>
public sealed record MediaDiscoveryUpdate(
    string CurrentPath,
    int FilesFound,
    Exception? Error = null);
