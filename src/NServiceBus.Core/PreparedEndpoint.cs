namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Features;
    using Installation;
    using ObjectBuilder;
    using Settings;
    using Transport;

    class PreparedEndpoint
    {
        ReceiveComponent receiveComponent;
        QueueBindings queueBindings;
        FeatureActivator featureActivator;
        TransportInfrastructure transportInfrastructure;
        CriticalError criticalError;
        SettingsHolder settings;
        PipelineComponent pipelineComponent;

        public PreparedEndpoint(ReceiveComponent receiveComponent, QueueBindings queueBindings, FeatureActivator featureActivator, TransportInfrastructure transportInfrastructure, CriticalError criticalError, SettingsHolder settings, PipelineComponent pipelineComponent)
        {
            this.receiveComponent = receiveComponent;
            this.queueBindings = queueBindings;
            this.featureActivator = featureActivator;
            this.transportInfrastructure = transportInfrastructure;
            this.criticalError = criticalError;
            this.settings = settings;
            this.pipelineComponent = pipelineComponent;
        }

        public async Task<IStartableEndpoint> Initialize(IBuilder builder)
        {
            pipelineComponent.InitializeBuilder(builder);

            var shouldRunInstallers = settings.GetOrDefault<bool>("Installers.Enable");

            if (shouldRunInstallers)
            {
                var username = GetInstallationUserName();

                if (settings.CreateQueues())
                {
                    await receiveComponent.CreateQueuesIfNecessary(queueBindings, username).ConfigureAwait(false);
                }

                await RunInstallers(builder, username).ConfigureAwait(false);
            }

            var messageSession = new MessageSession(pipelineComponent.CreateRootContext(builder));

            return new StartableEndpoint(settings, builder, featureActivator, transportInfrastructure, receiveComponent, criticalError, messageSession);
        }

        async Task RunInstallers(IBuilder builder, string username)
        {
            foreach (var installer in builder.BuildAll<INeedToInstallSomething>())
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
    }
}