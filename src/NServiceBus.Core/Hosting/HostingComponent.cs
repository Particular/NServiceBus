#nullable enable
namespace NServiceBus;

using System;
using System.Net;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using Support;

partial class HostingComponent(HostingComponent.Configuration configuration, InstallerComponent installerComponent)
{
    static readonly ILog Logger = LogManager.GetLogger<HostingComponent>();

    internal Configuration Config => configuration;

    public static HostingComponent Initialize(Configuration configuration)
    {
        if (configuration.UsedLegacyDeterministicGuid)
        {
            Logger.WarnFormat(
                "The host ID is generated using MD5 for backward compatibility. To migrate to the new algorithm, enable the AppContext switch " +
                "'NServiceBus.Core.Hosting.UseV2DeterministicGuid' using one of the following methods:\n" +
                "  - Code: AppContext.SetSwitch(\"NServiceBus.Core.Hosting.UseV2DeterministicGuid\", true);\n" +
                "  - Project file: <RuntimeHostConfigurationOption Include=\"NServiceBus.Core.Hosting.UseV2DeterministicGuid\" Value=\"true\" />\n" +
                "  - Environment variable (.NET 9+): DOTNET_NServiceBus_Core_Hosting_UseV2DeterministicGuid=true\n" +
                "Note: Enabling this switch changes your host ID, which may cause duplicate endpoint instances in monitoring tools until old entries expire.");
        }

        var serviceCollection = configuration.Services;

        serviceCollection.AddSingleton(_ => configuration.HostInformation);
        serviceCollection.AddSingleton(_ => configuration.CriticalError);

        // Apply user registrations last, so that user overrides win.
        foreach (var registration in configuration.UserRegistrations)
        {
            registration(serviceCollection);
        }

        configuration.AddStartupDiagnosticsSection("Hosting", new
        {
            configuration.HostInformation.HostId,
            HostDisplayName = configuration.HostInformation.DisplayName,
            RuntimeEnvironment.MachineName,
            OSPlatform = Environment.OSVersion.Platform,
            OSVersion = Environment.OSVersion.VersionString,
            GCSettings.IsServerGC,
            GCLatencyMode = GCSettings.LatencyMode,
            Environment.ProcessorCount,
            Environment.Is64BitProcess,
            CLRVersion = Environment.Version,
            Environment.WorkingSet,
            Environment.SystemPageSize,
            HostName = Dns.GetHostName(),
            Environment.UserName,
            PathToExe = PathUtilities.SanitizedPath(Environment.CommandLine),
            InstallersEnabled = configuration.ShouldRunInstallers
        });

        return new HostingComponent(configuration, configuration.InstallerComponent);
    }

    public async Task RunInstallers(IServiceProvider serviceProvider, bool forceInstallers = false, CancellationToken cancellationToken = default)
    {
        if (!configuration.ShouldRunInstallers && !forceInstallers)
        {
            return;
        }

        await installerComponent.RunInstallers(serviceProvider, cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteDiagnosticsFile(CancellationToken cancellationToken = default)
    {
        var hostStartupDiagnosticsWriter = HostStartupDiagnosticsWriterFactory.GetDiagnosticsWriter(configuration);

        await hostStartupDiagnosticsWriter.Write(configuration.StartupDiagnostics.entries, cancellationToken).ConfigureAwait(false);
    }

    public void SetupCriticalErrors(RunningEndpointInstance endpointInstance, CancellationToken cancellationToken = default) =>
        configuration.CriticalError.SetEndpoint(endpointInstance, cancellationToken);
}