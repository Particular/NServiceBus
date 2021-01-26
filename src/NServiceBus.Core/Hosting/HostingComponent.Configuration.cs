namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Hosting;
    using Installation;
    using Microsoft.Extensions.DependencyInjection;

    partial class HostingComponent
    {
        static bool IsINeedToInstallSomething(Type t) => typeof(INeedToInstallSomething).IsAssignableFrom(t);

        public static Configuration PrepareConfiguration(Settings settings, AssemblyScanningComponent assemblyScanningComponent, IServiceCollection serviceCollection)
        {
            var availableTypes = assemblyScanningComponent.AvailableTypes.Where(t => !t.IsAbstract && !t.IsInterface).ToList();

            var configuration = new Configuration(settings,
                availableTypes,
                new CriticalError(settings.CustomCriticalErrorAction),
                settings.StartupDiagnostics,
                settings.DiagnosticsPath,
                settings.HostDiagnosticsWriter,
                settings.EndpointName,
                serviceCollection,
                settings.InstallationUserName,
                settings.ShouldRunInstallers,
                settings.UserRegistrations);

            return configuration;
        }

        public class Configuration
        {
            public Configuration(Settings settings,
                List<Type> availableTypes,
                CriticalError criticalError,
                StartupDiagnosticEntries startupDiagnostics,
                string diagnosticsPath,
                Func<string, CancellationToken, Task> hostDiagnosticsWriter,
                string endpointName,
                IServiceCollection services,
                string installationUserName,
                bool shouldRunInstallers,
                List<Action<IServiceCollection>> userRegistrations)
            {
                AvailableTypes = availableTypes;
                CriticalError = criticalError;
                StartupDiagnostics = startupDiagnostics;
                DiagnosticsPath = diagnosticsPath;
                HostDiagnosticsWriter = hostDiagnosticsWriter;
                EndpointName = endpointName;
                Services = services;
                InstallationUserName = installationUserName;
                ShouldRunInstallers = shouldRunInstallers;
                UserRegistrations = userRegistrations;

                settings.ApplyHostIdDefaultIfNeeded();
                HostInformation = new HostInformation(settings.HostId, settings.DisplayName, settings.Properties);
            }

            public ICollection<Type> AvailableTypes { get; }

            public CriticalError CriticalError { get; }

            public StartupDiagnosticEntries StartupDiagnostics { get; }

            public Func<string, CancellationToken, Task> HostDiagnosticsWriter { get; }

            public void AddInstaller(Func<string, Task> installer)
            {
                internalInstallers.Add(installer);
            }

            public string EndpointName { get; }

            public IServiceCollection Services { get; }

            public string DiagnosticsPath { get; }

            public void AddStartupDiagnosticsSection(string sectionName, object section)
            {
                StartupDiagnostics.Add(sectionName, section);
            }

            public HostInformation HostInformation { get; }

            public bool ShouldRunInstallers { get; }

            public string InstallationUserName { get; }

            public List<Action<IServiceCollection>> UserRegistrations { get; }

            internal ICollection<Func<string, Task>> internalInstallers = new List<Func<string, Task>>();
        }
    }
}