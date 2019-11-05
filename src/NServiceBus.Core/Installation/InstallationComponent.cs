namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Installation;
    using Settings;
    using Transport;

    class InstallationComponent
    {
        InstallationComponent(Configuration configuration, ContainerComponent containerComponent, ReceiveComponent receiveComponent, QueueBindings queueBindings)
        {
            this.configuration = configuration;
            this.containerComponent = containerComponent;
            this.receiveComponent = receiveComponent;
            this.queueBindings = queueBindings;
        }

        public static InstallationComponent Initialize(Configuration configuration, List<Type> concreteTypes, ContainerComponent containerComponent, ReceiveComponent receiveComponent, QueueBindings queueBindings)
        {
            var component = new InstallationComponent(configuration, containerComponent, receiveComponent, queueBindings);

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

        public async Task Start()
        {
            if (!configuration.ShouldRunInstallers)
            {
                return;
            }

            var installationUserName = GetInstallationUserName();

            if (configuration.ShouldCreateQueues)
            {
                await receiveComponent.CreateQueuesIfNecessary(queueBindings, installationUserName).ConfigureAwait(false);
            }

            foreach (var installer in containerComponent.Builder.BuildAll<INeedToInstallSomething>())
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
        ContainerComponent containerComponent;
        ReceiveComponent receiveComponent;
        QueueBindings queueBindings;

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