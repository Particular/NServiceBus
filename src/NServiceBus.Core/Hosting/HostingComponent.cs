namespace NServiceBus
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
                container);

            container.ConfigureComponent(() => configuration.HostInformation, DependencyLifecycle.SingleInstance);
            container.ConfigureComponent(() => configuration.CriticalError, DependencyLifecycle.SingleInstance);

            foreach (var registration in settings.UserRegistrations)
            {
                registration(container);
            }

            return configuration;
        }

        public static HostingComponent Initialize(Configuration configuration, IBuilder internalBuilder)
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

            return new HostingComponent(configuration, internalBuilder);
        }

        public HostingComponent(Configuration configuration, IBuilder internalBuilder)
        {
            this.configuration = configuration;
            this.internalBuilder = internalBuilder;
        }

        public async Task<IEndpointInstance> Start(IStartableEndpoint startableEndpoint)
        {
            var hostStartupDiagnosticsWriter = HostStartupDiagnosticsWriterFactory.GetDiagnosticsWriter(configuration);

            await hostStartupDiagnosticsWriter.Write(configuration.StartupDiagnostics.entries).ConfigureAwait(false);

            var endpointInstance = await startableEndpoint.Start().ConfigureAwait(false);

            configuration.CriticalError.SetEndpoint(endpointInstance);

            return endpointInstance;
        }

        public Task Stop()
        {
            internalBuilder?.Dispose();

            return Task.FromResult(0);
        }

        Configuration configuration;
        IBuilder internalBuilder;

        public class Configuration
        {
            public Configuration(Settings settings,
                List<Type> availableTypes,
                CriticalError criticalError,
                StartupDiagnosticEntries startupDiagnostics,
                string diagnosticsPath,
                Func<string, Task> hostDiagnosticsWriter,
                string endpointName,
                IConfigureComponents container)
            {
                this.settings = settings;
                AvailableTypes = availableTypes;
                CriticalError = criticalError;
                StartupDiagnostics = startupDiagnostics;
                DiagnosticsPath = diagnosticsPath;
                HostDiagnosticsWriter = hostDiagnosticsWriter;
                EndpointName = endpointName;
                Container = container;
            }

            public ICollection<Type> AvailableTypes { get; }

            public CriticalError CriticalError { get; }

            public StartupDiagnosticEntries StartupDiagnostics { get; }

            public Func<string, Task> HostDiagnosticsWriter { get; }

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

            // We just need to do this to allow host id to be overidden by accessing settings via Feature defaults.
            // In v8 we can drop this and document in the upgrade guide that overriding host id is only supported via the public APIs
            // See the test When_feature_overrides_hostinfo for more details.
            [ObsoleteEx(RemoveInVersion = "8", TreatAsErrorFromVersion = "7")]
            public void CreateHostInformationForV7BackwardsCompatibility()
            {
                hostInformation = new HostInformation(settings.HostId, settings.DisplayName, settings.Properties);
            }

            HostInformation hostInformation;
            readonly Settings settings;
        }

        public class Settings
        {
            public Settings(SettingsHolder settings)
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

            public IContainer CustomObjectBuilder { get; set; }

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
