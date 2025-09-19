#nullable enable
namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Logging;

class EndpointManifestWriter(Func<string, CancellationToken, Task> manifestWriter, bool isCustomWriter)
{
    public async Task Write(ManifestItems manifest, CancellationToken cancellationToken = default)
    {
        string manifestData;
        try
        {
            manifestData = manifest.FormatJSON();
        }
        catch (Exception exception)
        {
            Logger.Error("Failed to serialize manifest data", exception);
            return;
        }
        try
        {
            await manifestWriter(manifestData, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
        {
            if (isCustomWriter)
            {
                Logger.Error($"Failed to write manifest data using the custom delegate defined by {nameof(ManifestConfigExtensions.EnableManifestGeneration)}", ex);
                return;
            }
            Logger.Error("Failed to write manifest data", ex);
        }
    }

    static readonly ILog Logger = LogManager.GetLogger<EndpointManifestWriter>();
}