namespace NServiceBus
{
    using System;
    using System.Net;
    using System.Runtime;
    using System.Threading.Tasks;
    using Installation;
    using ObjectBuilder;
    using Support;

    partial class HostingComponent
    {
        public HostingComponent(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public static HostingComponent Initialize(Configuration configuration)
        {
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

            return new HostingComponent(configuration);
        }

        public void RegisterBuilder(IBuilder objectBuilder, bool isInternalBuilder)
        {
            builder = objectBuilder;
            shouldDisposeBuilder = isInternalBuilder;
        }

        // This can't happent at start due to an old "feature" that allowed users to
        // run installers by "just creating the endpoint". See https://docs.particular.net/nservicebus/operations/installers#running-installers for more details.
        public async Task RunInstallers()
        {
            if (!configuration.ShouldRunInstallers)
            {
                return;
            }

            var installationUserName = GetInstallationUserName();

            foreach (var internalInstaller in configuration.internalInstallers)
            {
                await internalInstaller(installationUserName).ConfigureAwait(false);
            }

            foreach (var installer in builder.BuildAll<INeedToInstallSomething>())
            {
                await installer.Install(installationUserName).ConfigureAwait(false);
            }
        }

        public async Task<IEndpointInstance> Start(IStartableEndpoint startableEndpoint)
        {
            var hostStartupDiagnosticsWriter = HostStartupDiagnosticsWriterFactory.GetDiagnosticsWriter(configuration);

            await hostStartupDiagnosticsWriter.Write(configuration.StartupDiagnostics.entries).ConfigureAwait(false);

            var endpointInstance = await startableEndpoint.Start().ConfigureAwait(false);

            configuration.CriticalError.SetStopCallback(endpointInstance.Stop);

            return endpointInstance;
        }

        public Task Stop()
        {
            if (shouldDisposeBuilder)
            {
                builder.Dispose();
            }

            return Task.FromResult(0);
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
        bool shouldDisposeBuilder;
        IBuilder builder;
    }
}