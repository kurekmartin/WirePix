using System;
using System.Threading;
using System.Threading.Tasks;
using WirePix.Core.Import.Contracts;
using WirePix.Core.Models.Settings;
using WirePix.Core.Naming.Templates;
using WirePix.Devices.Contracts;

namespace WirePix.Core.Import;

public sealed class ImportSession(ImportCoordinator coordinator)
{
    private readonly ImportCoordinator _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
    private ImportCriteria? _criteria;

    public ImportPlan Plan { get; private set; }

    public async Task<ImportPlan> CreatePlanAsync(
        IMediaDevice device,
        DownloadSettings settings,
        IProgress<ImportProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        Plan = await _coordinator.CreatePlanAsync(device, settings, progress, cancellationToken).ConfigureAwait(false);
        _criteria = ImportCriteria.From(device, settings);
        return Plan;
    }

    public async Task<ImportResult> ExecuteAsync(
        IMediaDevice device,
        DownloadSettings settings,
        NamingContext naming,
        IProgress<ImportProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        ImportCriteria current = ImportCriteria.From(device, settings);
        if (Plan == null || _criteria != current)
        {
            await CreatePlanAsync(device, settings, progress, cancellationToken).ConfigureAwait(false);
        }
        return await _coordinator.ExecuteAsync(device, Plan, settings, naming, progress, cancellationToken).ConfigureAwait(false);
    }

    private readonly record struct ImportCriteria(string DeviceId, DateTime Start, DateTime End)
    {
        internal static ImportCriteria From(IMediaDevice device, DownloadSettings settings) =>
            new(device?.Id ?? string.Empty, settings?.Date?.Start ?? default, settings?.Date?.End ?? default);
    }
}
