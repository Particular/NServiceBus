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
        public HostingComponent(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public static HostingComponent Initialize(Configuration configuration)
        {
            var serviceCollection = configuration.Services;
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

            return new HostingComponent(configuration);
        }

        public async Task RunInstallers(IServiceProvider builder, CancellationToken cancellationToken = default)
        {
            var installationUserName = GetInstallationUserName();

            foreach (var installer in builder.GetServices<INeedToInstallSomething>())
            {
                await installer.Install(installationUserName, cancellationToken).ConfigureAwait(false);
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
    }
}