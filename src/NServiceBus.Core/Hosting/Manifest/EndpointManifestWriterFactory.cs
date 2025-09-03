#nullable enable
namespace NServiceBus;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Logging;

static class EndpointManifestWriterFactory
{
    public static EndpointManifestWriter GetEndpointManifestWriter(HostingComponent.Configuration configuration)
    {
        var manifestWriter = configuration.EndpointManifestWriter;
        manifestWriter ??= BuildDefaultManifestWriter(configuration);

        return new EndpointManifestWriter(manifestWriter, configuration.EndpointManifestWriter != null);
    }

    static Func<string, CancellationToken, Task> BuildDefaultManifestWriter(HostingComponent.Configuration configuration)
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
                return (_, __) => Task.CompletedTask;
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
                return (_, __) => Task.CompletedTask;
            }
        }

        // Once we have the proper hosting model in place we can skip the endpoint name since the host would
        // know how to handle multi hosting but for now we do this so that multi-hosting users will get a file per endpoint
        var startupManifestFileName = $"{configuration.EndpointName}-manifest.txt";
        var startupManifestFilePath = Path.Combine(manifestRootPath, startupManifestFileName);

        return async (data, cancellationToken) =>
        {
            await AsyncFile.WriteText(startupManifestFilePath, data, cancellationToken).ConfigureAwait(false);
            Logger.Info($"Manifest data written to '{startupManifestFilePath}'");
        };
    }

    static readonly ILog Logger = LogManager.GetLogger<EndpointManifestWriter>();
}