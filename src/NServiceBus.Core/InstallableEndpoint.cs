namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Features;
    using Installation;
    using ObjectBuilder;
    using Settings;
    using Transport;

    class InstallableEndpoint  : IInstallableEndpoint
    {
        public InstallableEndpoint(SettingsHolder settings, IBuilder builder, FeatureActivator featureActivator, TransportInfrastructure transportInfrastructure, ReceiveComponent receiveComponent, CriticalError criticalError, IMessageSession messageSession)
        {
            this.criticalError = criticalError;
            this.settings = settings;
            this.builder = builder;
            this.featureActivator = featureActivator;
            this.transportInfrastructure = transportInfrastructure;
            this.receiveComponent = receiveComponent;
            this.messageSession = messageSession;
        }

        public async Task<IStartableEndpoint> Install()
        {
            var queueBindings = settings.Get<QueueBindings>();

            var shouldRunInstallers = settings.GetOrDefault<bool>("Installers.Enable");
            if (shouldRunInstallers)
            {
                var username = GetInstallationUserName();

                if (settings.CreateQueues())
                {
                    await receiveComponent.CreateQueuesIfNecessary(queueBindings, username).ConfigureAwait(false);
                }

                await RunInstallers(username).ConfigureAwait(false);
            }

            return CreateStartableEndpoint();
        }

        public Task<IEndpointInstance> Start()
        {
            return CreateStartableEndpoint().Start();
        }

        StartableEndpoint CreateStartableEndpoint()
        {
            return new StartableEndpoint(settings, builder, featureActivator, transportInfrastructure, receiveComponent, criticalError, messageSession);
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

        async Task RunInstallers(string username)
        {
            foreach (var installer in builder.BuildAll<INeedToInstallSomething>())
            {
                await installer.Install(username).ConfigureAwait(false);
            }
        }

        IMessageSession messageSession;
        IBuilder builder;
        FeatureActivator featureActivator;
        SettingsHolder settings;
        TransportInfrastructure transportInfrastructure;
        ReceiveComponent receiveComponent;
        CriticalError criticalError;
    }
}