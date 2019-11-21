namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Installation;
    using ObjectBuilder;
    using Settings;

    class InstallationComponent
    {
        InstallationComponent(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public static InstallationComponent Initialize(Configuration configuration, HostingComponent.Configuration hostingConfiguration)
        {
            var component = new InstallationComponent(configuration);

            if (!configuration.ShouldRunInstallers)
            {
                return component;
            }

            foreach (var installerType in hostingConfiguration.AvailableTypes.Where(t => IsINeedToInstallSomething(t)))
            {
                hostingConfiguration.Container.ConfigureComponent(installerType, DependencyLifecycle.InstancePerCall);
            }

            return component;
        }

        public async Task Start(IBuilder builder)
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

        Configuration configuration;

        static bool IsINeedToInstallSomething(Type t) => typeof(INeedToInstallSomething).IsAssignableFrom(t);

        public class Configuration
        {
            public Configuration(SettingsHolder settings)
            {
                this.settings = settings;
            }

            public string InstallationUserName
            {
                get => settings.GetOrDefault<string>("Installers.UserName");
                set => settings.Set("Installers.UserName", value);
            }

            public bool ShouldRunInstallers
            {
                get => settings.GetOrDefault<bool>("Installers.Enable");
                set => settings.Set("Installers.Enable", value);
            }

            public void AddInstaller(Func<string, Task> installer)
            {
                internalInstallers.Add(installer);
            }

            SettingsHolder settings;
            internal List<Func<string, Task>> internalInstallers = new List<Func<string, Task>>();
        }
    }
}