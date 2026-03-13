#nullable enable
namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hosting;
using Microsoft.Extensions.DependencyInjection;

partial class HostingComponent
{
    public static Configuration PrepareConfiguration(Settings settings, List<Type> availableTypes,
        PersistenceComponent.Configuration persistenceConfiguration, InstallerComponent installerComponent,
        IServiceCollection serviceCollection)
    {
        var configuration = new Configuration(settings,
            availableTypes,
            new CriticalError(settings.CustomCriticalErrorAction),
            settings.StartupDiagnostics,
            settings.DiagnosticsPath,
            settings.HostDiagnosticsWriter,
            settings.GetOrCreateEndpointLogSlot(),
            settings.EndpointName,
            serviceCollection,
            settings.ShouldRunInstallers,
            new ActivityFactory(),
            persistenceConfiguration,
            installerComponent);

        return configuration;
    }

    public class Configuration
    {
        internal Configuration(Settings settings,
            List<Type> availableTypes,
            CriticalError criticalError,
            StartupDiagnosticEntries startupDiagnostics,
            string? diagnosticsPath,
            Func<string, CancellationToken, Task>? hostDiagnosticsWriter,
            EndpointLogSlot endpointLogSlot,
            string endpointName,
            IServiceCollection services,
            bool shouldRunInstallers,
            IActivityFactory activityFactory,
            PersistenceComponent.Configuration persistenceConfiguration,
            InstallerComponent installerComponent)
        {
            AvailableTypes = availableTypes;
            CriticalError = criticalError;
            StartupDiagnostics = startupDiagnostics;
            DiagnosticsPath = diagnosticsPath;
            HostDiagnosticsWriter = hostDiagnosticsWriter;
            EndpointLogSlot = endpointLogSlot;
            EndpointName = endpointName;
            Services = services;
            ShouldRunInstallers = shouldRunInstallers;
            ActivityFactory = activityFactory;
            PersistenceConfiguration = persistenceConfiguration;
            InstallerComponent = installerComponent;

            settings.ApplyHostIdDefaultIfNeeded();
            HostInformation = new HostInformation(settings.HostId, settings.DisplayName, settings.Properties);
        }

        public ICollection<Type> AvailableTypes { get; }

        public CriticalError CriticalError { get; }

        public StartupDiagnosticEntries StartupDiagnostics { get; }

        public Func<string, CancellationToken, Task>? HostDiagnosticsWriter { get; }

        internal EndpointLogSlot EndpointLogSlot { get; }

        public string EndpointName { get; }

        public IServiceCollection Services { get; }

        public string? DiagnosticsPath { get; }

        public void AddStartupDiagnosticsSection(string sectionName, object section) => StartupDiagnostics.Add(sectionName, section);

        public HostInformation HostInformation { get; }

        public bool ShouldRunInstallers { get; }

        public IActivityFactory ActivityFactory { get; }

        public PersistenceComponent.Configuration PersistenceConfiguration { get; }
        public InstallerComponent InstallerComponent { get; }
    }
}