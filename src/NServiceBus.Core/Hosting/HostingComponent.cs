#nullable enable
namespace NServiceBus;

using System;
using System.Net;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Support;

partial class HostingComponent(HostingComponent.Configuration configuration, InstallerComponent installerComponent)
{
    internal Configuration Config => configuration;

    public static HostingComponent Initialize(Configuration configuration)
    {
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
            InstallersEnabled = configuration.ShouldRunInstallers,
            Installers = configuration.InstallerComponent.GetDiagnostics()
        });

        return new HostingComponent(configuration, configuration.InstallerComponent);
    }

    public async Task RunInstallers(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        if (!configuration.ShouldRunInstallers)
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

    public void SetupCriticalErrors(IEndpointInstance endpointInstance, CancellationToken cancellationToken = default) =>
        configuration.CriticalError.SetEndpoint(endpointInstance, cancellationToken);
}