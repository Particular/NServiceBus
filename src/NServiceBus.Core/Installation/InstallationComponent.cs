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
        InstallationComponent(Configuration configuration, TransportComponent transportComponent)
        {
            this.configuration = configuration;
            this.transportComponent = transportComponent;
        }

        public static InstallationComponent Initialize(Configuration configuration, List<Type> concreteTypes, ContainerComponent containerComponent, TransportComponent transportComponent)
        {
            var component = new InstallationComponent(configuration, transportComponent);

            if (!configuration.ShouldRunInstallers)
            {
                return component;
            }

            foreach (var installerType in concreteTypes.Where(t => IsINeedToInstallSomething(t)))
            {
                containerComponent.ContainerConfiguration.ConfigureComponent(installerType, DependencyLifecycle.InstancePerCall);
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

            if (configuration.ShouldCreateQueues)
            {
                await transportComponent.CreateQueuesIfNecessary(installationUserName).ConfigureAwait(false);
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
        TransportComponent transportComponent;

        static bool IsINeedToInstallSomething(Type t) => typeof(INeedToInstallSomething).IsAssignableFrom(t);

        public class Configuration
        {
            public Configuration(SettingsHolder settings)
            {
                this.settings = settings;

                settings.SetDefault("Transport.CreateQueues", true);
            }

            public string InstallationUserName
            {
                get
                {
                    return settings.GetOrDefault<string>("Installers.UserName");
                }
                set
                {
                    settings.Set("Installers.UserName", value);
                }
            }

            public bool ShouldRunInstallers
            {
                get
                {
                    return settings.GetOrDefault<bool>("Installers.Enable");
                }
                set
                {
                    settings.Set("Installers.Enable", value);
                }
            }

            public bool ShouldCreateQueues
            {
                get
                {
                    return settings.Get<bool>("Transport.CreateQueues");
                }
                set
                {
                    settings.Set("Transport.CreateQueues", value);
                }
            }

            SettingsHolder settings;
        }
    }
}