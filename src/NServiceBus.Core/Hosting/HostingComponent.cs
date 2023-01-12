namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Runtime;
    using System.Threading;
    using System.Threading.Tasks;
    using Installation;
    using Microsoft.Extensions.DependencyInjection;
    using Support;

    partial class HostingComponent
    {
        public HostingComponent(Configuration configuration, bool shouldDisposeServiceProvider)
        {
            this.configuration = configuration;
            this.shouldDisposeServiceProvider = shouldDisposeServiceProvider;
        }

        public static HostingComponent Initialize(Configuration configuration, IServiceCollection serviceCollection, bool shouldDisposeServiceProvider)
        {
            serviceCollection.ConfigureComponent(() => configuration.HostInformation, DependencyLifecycle.SingleInstance);
            serviceCollection.ConfigureComponent(() => configuration.CriticalError, DependencyLifecycle.SingleInstance);

            foreach (var installerType in configuration.AvailableTypes.Where(t => IsINeedToInstallSomething(t)))
            {
                serviceCollection.ConfigureComponent(installerType, DependencyLifecycle.InstancePerCall);
            }

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
                PathToExe = PathUtilities.SanitizedPath(Environment.CommandLine)
            });

            return new HostingComponent(configuration, shouldDisposeServiceProvider);
        }

        public void RegisterServiceProvider(IServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

        // This can't happen at start due to an old "feature" that allowed users to
        // run installers by "just creating the endpoint". See https://docs.particular.net/nservicebus/operations/installers#running-installers for more details.
        public async Task RunInstallers(CancellationToken cancellationToken = default)
        {
            if (!configuration.ShouldRunInstallers)
            {
                return;
            }

            var installationUserName = GetInstallationUserName();

            foreach (var installer in serviceProvider.GetServices<INeedToInstallSomething>())
            {
                await installer.Install(installationUserName, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IEndpointInstance> Start(IStartableEndpoint startableEndpoint, CancellationToken cancellationToken = default)
        {
            var hostStartupDiagnosticsWriter = HostStartupDiagnosticsWriterFactory.GetDiagnosticsWriter(configuration);

            await hostStartupDiagnosticsWriter.Write(configuration.StartupDiagnostics.entries, cancellationToken).ConfigureAwait(false);

            var endpointInstance = await startableEndpoint.Start(cancellationToken).ConfigureAwait(false);

            configuration.CriticalError.SetEndpoint(endpointInstance, cancellationToken);

            return endpointInstance;
        }

        public async Task Stop(CancellationToken cancellationToken = default)
        {
            if (shouldDisposeServiceProvider && serviceProvider is IAsyncDisposable asyncDisposableBuilder)
            {
                await asyncDisposableBuilder.DisposeAsync().ConfigureAwait(false);
            }
        }

        string GetInstallationUserName()
        {
            if (configuration.InstallationUserName != null)
            {
                return configuration.InstallationUserName;
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return $"{Environment.UserDomainName}\\{Environment.UserName}";
            }

            return Environment.UserName;
        }

        readonly Configuration configuration;
        bool shouldDisposeServiceProvider;
        IServiceProvider serviceProvider;
    }
}