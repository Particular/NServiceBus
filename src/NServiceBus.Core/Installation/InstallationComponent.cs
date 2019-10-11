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

        public static InstallationComponent Initialize(Configuration settings, List<Type> concreteTypes, ContainerComponent containerComponent, ReceiveComponent receiveComponent, QueueBindings queueBindings)
        {
            var component = new InstallationComponent(settings, containerComponent, receiveComponent, queueBindings);

            if (!settings.ShouldRunInstallers)
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
            public Configuration(ReadOnlySettings settings)
            {
                InstallationUserName = settings.GetOrDefault<string>("Installers.UserName");
                ShouldRunInstallers = settings.GetOrDefault<bool>("Installers.Enable");
                ShouldCreateQueues = settings.CreateQueues();
            }

            public string InstallationUserName { get; }
            public bool ShouldRunInstallers { get; }
            public bool ShouldCreateQueues { get; }
        }
    }
}