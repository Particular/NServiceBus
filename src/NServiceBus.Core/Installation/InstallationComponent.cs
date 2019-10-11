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
        InstallationComponent(ContainerComponent containerComponent, ReceiveComponent receiveComponent, bool shouldRunInstallers, bool shouldCreateQueues, string installationUserName, QueueBindings queueBindings)
        {
            this.containerComponent = containerComponent;
            this.receiveComponent = receiveComponent;
            this.shouldRunInstallers = shouldRunInstallers;
            this.shouldCreateQueues = shouldCreateQueues;
            this.installationUserName = installationUserName;
            this.queueBindings = queueBindings;
        }

        public static InstallationComponent Initialize(ReadOnlySettings settings, List<Type> concreteTypes, ContainerComponent containerComponent, ReceiveComponent receiveComponent)
        {
            var shouldRunInstallers = settings.GetOrDefault<bool>("Installers.Enable");
            var installationUserName = GetInstallationUserName(settings);

            var component = new InstallationComponent(containerComponent, receiveComponent, shouldRunInstallers, settings.CreateQueues(), installationUserName, settings.Get<QueueBindings>());


            if (!shouldRunInstallers)
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
            if (!shouldRunInstallers)
            {
                return;
            }

            if (shouldCreateQueues)
            {
                await receiveComponent.CreateQueuesIfNecessary(queueBindings, installationUserName).ConfigureAwait(false);
            }

            foreach (var installer in containerComponent.Builder.BuildAll<INeedToInstallSomething>())
            {
                await installer.Install(installationUserName).ConfigureAwait(false);
            }
        }

        static string GetInstallationUserName(ReadOnlySettings settings)
        {
            if (!settings.TryGet("Installers.UserName", out string userName))
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    userName = $"{Environment.UserDomainName}\\{Environment.UserName}";
                }
                else
                {
                    userName = Environment.UserName;
                }
            }

            return userName;
        }

        ContainerComponent containerComponent;
        ReceiveComponent receiveComponent;
        bool shouldRunInstallers;
        bool shouldCreateQueues;
        string installationUserName;
        QueueBindings queueBindings;

        static bool IsINeedToInstallSomething(Type t) => typeof(INeedToInstallSomething).IsAssignableFrom(t);
    }
}