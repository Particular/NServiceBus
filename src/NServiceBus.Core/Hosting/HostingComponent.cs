﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Runtime;
    using System.Threading.Tasks;
    using Hosting;
    using ObjectBuilder;
    using ObjectBuilder.Common;
    using Settings;
    using Support;

    class HostingComponent
    {
        public HostingComponent(Configuration configuration, List<Type> availableTypes, IConfigureComponents container, IBuilder internalBuilder)
        {
            this.configuration = configuration;
            this.internalBuilder = internalBuilder;
            AvailableTypes = availableTypes;
            Container = container;
            CriticalError = new CriticalError(configuration.CustomCriticalErrorAction);
        }

        public string EndpointName => configuration.EndpointName;

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

        public CriticalError CriticalError { get; }

        public IConfigureComponents Container { get; private set; }

        public ICollection<Type> AvailableTypes { get; }

        public static HostingComponent Initialize(Configuration configuration,
            AssemblyScanningComponent assemblyScanningComponent,
            IConfigureComponents internalContainer,
            IBuilder internalBuilder)
        {
            var availableTypes = assemblyScanningComponent.AvailableTypes.Where(t => !t.IsAbstract && !t.IsInterface).ToList();

            var hostingComponent = new HostingComponent(configuration, availableTypes, internalContainer, internalBuilder);

            ApplyRegistrations(configuration, hostingComponent);

            return hostingComponent;
        }

        public void AddStartupDiagnosticsSection(string sectionName, object section)
        {
            configuration.StartupDiagnostics.Add(sectionName, section);
        }

        // We just need to do this to allow host id to be overidden by accessing settings via Feature defaults.
        // In v8 we can drop this and document in the upgrade guide that overriding host id is only supported via the public APIs
        // See the test When_feature_overrides_hostinfo for more details.
        [ObsoleteEx(RemoveInVersion = "8", TreatAsErrorFromVersion = "7")]
        public void CreateHostInformationForV7BackwardsCompatibility()
        {
            hostInformation = new HostInformation(configuration.HostId, configuration.DisplayName, configuration.Properties);

            AddStartupDiagnosticsSection("Hosting", new
            {
                hostInformation.HostId,
                HostDisplayName = hostInformation.DisplayName,
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
        }

        public Task Start(IEndpointInstance endpointInstance)
        {
            CriticalError.SetEndpoint(endpointInstance);

            var hostStartupDiagnosticsWriter = HostStartupDiagnosticsWriterFactory.GetDiagnosticsWriter(configuration);

            return hostStartupDiagnosticsWriter.Write(configuration.StartupDiagnostics.entries);
        }

        public Task Stop()
        {
            internalBuilder?.Dispose();

            return Task.FromResult(0);
        }

        static void ApplyRegistrations(Configuration configuration, HostingComponent hostingComponent)
        {
            hostingComponent.Container.ConfigureComponent(() => hostingComponent.HostInformation, DependencyLifecycle.SingleInstance);
            hostingComponent.Container.ConfigureComponent(() => hostingComponent.CriticalError, DependencyLifecycle.SingleInstance);

            foreach (var registration in configuration.UserRegistrations)
            {
                registration(hostingComponent.Container);
            }
        }

        Configuration configuration;
        HostInformation hostInformation;
        IBuilder internalBuilder;

        public class Configuration
        {
            public Configuration(SettingsHolder settings)
            {
                this.settings = settings;

                fullPathToStartingExe = PathUtilities.SanitizedPath(Environment.CommandLine);

                settings.SetDefault(DisplayNameSettingsKey, RuntimeEnvironment.MachineName);
                settings.SetDefault(PropertiesSettingsKey, new Dictionary<string, string>
                {
                    {"Machine", RuntimeEnvironment.MachineName},
                    {"ProcessID", Process.GetCurrentProcess().Id.ToString()},
                    {"UserName", Environment.UserName},
                    {"PathToExecutable", fullPathToStartingExe}
                });

                settings.Set(new StartupDiagnosticEntries());
            }

            public Guid HostId
            {
                get { return settings.Get<Guid>(HostIdSettingsKey); }
                set { settings.Set(HostIdSettingsKey, value); }
            }

            public string DisplayName
            {
                get { return settings.Get<string>(DisplayNameSettingsKey); }
                set { settings.Set(DisplayNameSettingsKey, value); }
            }

            public string EndpointName
            {
                get { return settings.EndpointName(); }
            }

            public Dictionary<string, string> Properties
            {
                get { return settings.Get<Dictionary<string, string>>(PropertiesSettingsKey); }
                set { settings.Set(PropertiesSettingsKey, value); }
            }

            public StartupDiagnosticEntries StartupDiagnostics
            {
                get { return settings.Get<StartupDiagnosticEntries>(); }
                set { settings.Set(value); }
            }

            public string DiagnosticsPath
            {
                get { return settings.GetOrDefault<string>(DiagnosticsPathSettingsKey); }
                set { settings.Set(DiagnosticsPathSettingsKey, value); }
            }

            public Func<string, Task> HostDiagnosticsWriter
            {
                get { return settings.GetOrDefault<Func<string, Task>>(HostDiagnosticsWriterSettingsKey); }
                set { settings.Set(HostDiagnosticsWriterSettingsKey, value); }
            }

            public Func<ICriticalErrorContext, Task> CustomCriticalErrorAction
            {
                get
                {
                    return settings.GetOrDefault<Func<ICriticalErrorContext, Task>>(CustomCriticalErrorActionSettingsKey);
                }
                set
                {
                    settings.Set(CustomCriticalErrorActionSettingsKey, value);
                }
            }

            public List<Action<IConfigureComponents>> UserRegistrations { get; } = new List<Action<IConfigureComponents>>();

            public IContainer CustomContainer { get; set; }

            // Since the host id default is using MD5 which breaks MIPS compliant users we need to delay setting the default until users have a chance to override
            // via a custom feature to be backwards compatible.
            // For more details see the test: When_feature_overrides_hostid_from_feature_default
            // When this is removed in v8 downstreams can no longer rely on the setting to always be there
            [ObsoleteEx(RemoveInVersion = "8", TreatAsErrorFromVersion = "7")]
            internal void ApplyHostIdDefaultIfNeededForV7BackwardsCompatibility()
            {
                if (settings.HasExplicitValue(HostIdSettingsKey))
                {
                    return;
                }

                settings.SetDefault(HostIdSettingsKey, DeterministicGuid.Create(fullPathToStartingExe, RuntimeEnvironment.MachineName));
            }

            SettingsHolder settings;
            string fullPathToStartingExe;

            const string HostIdSettingsKey = "NServiceBus.HostInformation.HostId";
            const string DisplayNameSettingsKey = "NServiceBus.HostInformation.DisplayName";
            const string PropertiesSettingsKey = "NServiceBus.HostInformation.Properties";
            const string DiagnosticsPathSettingsKey = "Diagnostics.RootPath";
            const string HostDiagnosticsWriterSettingsKey = "HostDiagnosticsWriter";
            const string CustomCriticalErrorActionSettingsKey = "onCriticalErrorAction";
        }
    }
}
