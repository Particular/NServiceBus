namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Hosting;
    using Installation;
    using Microsoft.Extensions.DependencyInjection;
    using ObjectBuilder;

    partial class HostingComponent
    {
        static bool IsINeedToInstallSomething(Type t) => typeof(INeedToInstallSomething).IsAssignableFrom(t);

        public static Configuration PrepareConfiguration(Settings settings, AssemblyScanningComponent assemblyScanningComponent, IServiceCollection serviceCollection)
        {
            var availableTypes = assemblyScanningComponent.AvailableTypes.Where(t => !t.IsAbstract && !t.IsInterface).ToList();
            var configureComponentsAdapter = new CommonObjectBuilder(serviceCollection);

            var configuration = new Configuration(settings,
                availableTypes,
                new CriticalError(settings.CustomCriticalErrorAction),
                settings.StartupDiagnostics,
                settings.DiagnosticsPath,
                settings.HostDiagnosticsWriter,
                settings.EndpointName,
                configureComponentsAdapter,
                settings.InstallationUserName,
                settings.ShouldRunInstallers);

            configureComponentsAdapter.ConfigureComponent(() => configuration.HostInformation, DependencyLifecycle.SingleInstance);
            configureComponentsAdapter.ConfigureComponent(() => configuration.CriticalError, DependencyLifecycle.SingleInstance);

            foreach (var installerType in availableTypes.Where(t => IsINeedToInstallSomething(t)))
            {
                configureComponentsAdapter.ConfigureComponent(installerType, DependencyLifecycle.InstancePerCall);
            }

            foreach (var registration in settings.UserRegistrations)
            {
                registration(configureComponentsAdapter);
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
                AvailableTypes = availableTypes;
                CriticalError = criticalError;
                StartupDiagnostics = startupDiagnostics;
                DiagnosticsPath = diagnosticsPath;
                HostDiagnosticsWriter = hostDiagnosticsWriter;
                EndpointName = endpointName;
                Container = container;
                InstallationUserName = installationUserName;
                ShouldRunInstallers = shouldRunInstallers;

                settings.ApplyHostIdDefaultIfNeeded();
                HostInformation = new HostInformation(settings.HostId, settings.DisplayName, settings.Properties);
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

            public HostInformation HostInformation { get; }

            public bool ShouldRunInstallers { get; }

            public string InstallationUserName { get; }

            internal ICollection<Func<string, Task>> internalInstallers = new List<Func<string, Task>>();
        }
    }
}