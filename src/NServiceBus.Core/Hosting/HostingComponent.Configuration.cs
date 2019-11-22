namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Hosting;
    using Installation;
    using ObjectBuilder;

    partial class HostingComponent
    {
        static bool IsINeedToInstallSomething(Type t) => typeof(INeedToInstallSomething).IsAssignableFrom(t);

        public static Configuration PrepareConfiguration(Settings settings, AssemblyScanningComponent assemblyScanningComponent, IConfigureComponents container)
        {
            var availableTypes = assemblyScanningComponent.AvailableTypes.Where(t => !t.IsAbstract && !t.IsInterface).ToList();

            var configuration = new Configuration(settings,
                availableTypes,
                new CriticalError(settings.CustomCriticalErrorAction),
                settings.StartupDiagnostics,
                settings.DiagnosticsPath,
                settings.HostDiagnosticsWriter,
                settings.EndpointName,
                container,
                settings.InstallationUserName,
                settings.ShouldRunInstallers);

            container.ConfigureComponent(() => configuration.HostInformation, DependencyLifecycle.SingleInstance);
            container.ConfigureComponent(() => configuration.CriticalError, DependencyLifecycle.SingleInstance);

            foreach (var installerType in availableTypes.Where(t => IsINeedToInstallSomething(t)))
            {
                container.ConfigureComponent(installerType, DependencyLifecycle.InstancePerCall);
            }

            foreach (var registration in settings.UserRegistrations)
            {
                registration(container);
            }

            return configuration;
        }

        public class Configuration
        {
            public Configuration(Settings settings,
                List<Type> availableTypes,
                CriticalError criticalError,
                StartupDiagnosticEntries startupDiagnostics,
                string diagnosticsPath,
                Func<string, Task> hostDiagnosticsWriter,
                string endpointName,
                IConfigureComponents container,
                string installationUserName,
                bool shouldRunInstallers)
            {
                this.settings = settings;

                AvailableTypes = availableTypes;
                CriticalError = criticalError;
                StartupDiagnostics = startupDiagnostics;
                DiagnosticsPath = diagnosticsPath;
                HostDiagnosticsWriter = hostDiagnosticsWriter;
                EndpointName = endpointName;
                Container = container;
                InstallationUserName = installationUserName;
                ShouldRunInstallers = shouldRunInstallers;
            }

            public ICollection<Type> AvailableTypes { get; }

            public CriticalError CriticalError { get; }

            public StartupDiagnosticEntries StartupDiagnostics { get; }

            public Func<string, Task> HostDiagnosticsWriter { get; }

            public void AddInstaller(Func<string, Task> installer)
            {
                internalInstallers.Add(installer);
            }

            public string EndpointName { get; }

            public IConfigureComponents Container { get; }

            public string DiagnosticsPath { get; }

            public void AddStartupDiagnosticsSection(string sectionName, object section)
            {
                StartupDiagnostics.Add(sectionName, section);
            }

            public HostInformation HostInformation
            {
                get
                {
                    if (hostInformation == null)
                    {
                        throw new InvalidOperationException("Host information can't be accessed until features have been created for backwards compatibility");
                    }

                    return hostInformation;
                }
            }

            public bool ShouldRunInstallers { get; }

            public string InstallationUserName { get; }

            // We just need to do this to allow host id to be overidden by accessing settings via Feature defaults.
            // In v8 we can drop this and document in the upgrade guide that overriding host id is only supported via the public APIs
            // See the test When_feature_overrides_hostinfo for more details.
            [ObsoleteEx(RemoveInVersion = "8", TreatAsErrorFromVersion = "7")]
            public void CreateHostInformationForV7BackwardsCompatibility()
            {
                hostInformation = new HostInformation(settings.HostId, settings.DisplayName, settings.Properties);
            }

            internal ICollection<Func<string, Task>> internalInstallers = new List<Func<string, Task>>();
            HostInformation hostInformation;
            readonly Settings settings;
        }
    }
}