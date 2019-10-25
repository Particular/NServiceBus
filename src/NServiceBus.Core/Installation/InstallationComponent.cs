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
        public InstallationComponent(ReadOnlySettings settings)
        {
            this.settings = settings;
        }

        public void Initialize(List<Type> concreteTypes, ContainerComponent container, ReceiveComponent receiver)
        {
            containerComponent = container;
            receiveComponent = receiver;

            shouldRunInstallers = settings.GetOrDefault<bool>("Installers.Enable");

            if (!shouldRunInstallers)
            {
                return;
            }

            foreach (var installerType in concreteTypes.Where(t => IsINeedToInstallSomething(t)))
            {
                containerComponent.ContainerConfiguration.ConfigureComponent(installerType, DependencyLifecycle.InstancePerCall);
            }
        }

        public async Task Start()
        {
            if (!shouldRunInstallers)
            {
                return;
            }

            var queueBindings = settings.Get<QueueBindings>();
            var username = GetInstallationUserName();

            if (settings.CreateQueues())
            {
                await receiveComponent.CreateQueuesIfNecessary(queueBindings, username).ConfigureAwait(false);
            }

            foreach (var installer in containerComponent.Builder.BuildAll<INeedToInstallSomething>())
            {
                await installer.Install(username).ConfigureAwait(false);
            }
        }

        string GetInstallationUserName()
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

        ReadOnlySettings settings;
        ContainerComponent containerComponent;
        ReceiveComponent receiveComponent;
        bool shouldRunInstallers;

        static bool IsINeedToInstallSomething(Type t) => typeof(INeedToInstallSomething).IsAssignableFrom(t);
    }
}