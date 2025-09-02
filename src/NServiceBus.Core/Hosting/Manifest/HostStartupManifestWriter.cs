#nullable enable
namespace NServiceBus;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Logging;

class HostStartupManifestWriter(HostingComponent.Configuration configuration)
{
    public async Task Write(ManifestItem manifest, CancellationToken cancellationToken = default)
    {
        var manifestRootPath = configuration.ManifestPath;

        if (manifestRootPath == null)
        {
            try
            {
                manifestRootPath = Path.Combine(Host.GetOutputDirectory(), ".manifest");
            }
            catch (Exception e)
            {
                Logger.Warn("Unable to determine the manifest output directory. Check the attached exception for further information.", e);
                return;
            }
        }

        if (!Directory.Exists(manifestRootPath))
        {
            try
            {
                Directory.CreateDirectory(manifestRootPath);
            }
            catch (Exception e)
            {
                Logger.Warn("Unable to create the manifest output directory. Check the attached exception for further information.", e);

                return;
            }
        }

        // Once we have the proper hosting model in place we can skip the endpoint name since the host would
        // know how to handle multi hosting but for now we do this so that multi-hosting users will get a file per endpoint
        var startupManifestFileName = $"{configuration.EndpointName}-manifest.txt";
        var startupManifestFilePath = Path.Combine(manifestRootPath, startupManifestFileName);

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
            await AsyncFile.WriteText(startupManifestFilePath, manifestData, cancellationToken).ConfigureAwait(false);
            Logger.Info($"Manifest data written to '{startupManifestFilePath}'");
        }
        catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
        {
            Logger.Error("Failed to write manifest data", ex);
        }
    }

    static readonly ILog Logger = LogManager.GetLogger<HostStartupManifestWriter>();
}