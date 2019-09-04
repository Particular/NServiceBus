namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Features;
    using Installation;
    using ObjectBuilder;
    using Settings;
    using Transport;

    /// <summary>
    /// A prepared endpoint using an external container.
    /// </summary>
    public class PreparedEndpoint
    {
        /// <summary>
        /// Provides access to the endpoints message session 
        /// Note: The message session is only valid to use once the endpoint have been started.
        /// </summary>
        public Lazy<IMessageSession> MessageSession { get; }

        internal PreparedEndpoint(ReceiveComponent receiveComponent, QueueBindings queueBindings, FeatureActivator featureActivator, TransportInfrastructure transportInfrastructure, CriticalError criticalError, SettingsHolder settings, PipelineComponent pipelineComponent, ContainerComponent containerComponent)
        {
            this.receiveComponent = receiveComponent;
            this.queueBindings = queueBindings;
            this.featureActivator = featureActivator;
            this.transportInfrastructure = transportInfrastructure;
            this.criticalError = criticalError;
            this.settings = settings;
            this.pipelineComponent = pipelineComponent;
            this.containerComponent = containerComponent;

            MessageSession = new Lazy<IMessageSession>(() =>
            {
                if (messageSession == null)
                {
                    throw new InvalidOperationException("The message session can only be used after the endpoint is started.");
                }
                return messageSession;
            });
        }

        internal void UseExternallyManagedBuilder(IBuilder builder)
        {
            containerComponent.UseExternallyManagedBuilder(builder);
        }

        internal async Task<IStartableEndpoint> Initialize()
        {
            pipelineComponent.Initialize(containerComponent.Builder);

            var shouldRunInstallers = settings.GetOrDefault<bool>("Installers.Enable");

            if (shouldRunInstallers)
            {
                var username = GetInstallationUserName();

                if (settings.CreateQueues())
                {
                    await receiveComponent.CreateQueuesIfNecessary(queueBindings, username).ConfigureAwait(false);
                }

                await RunInstallers(containerComponent.Builder, username).ConfigureAwait(false);
            }

            messageSession = new MessageSession(pipelineComponent.CreateRootContext(containerComponent.Builder));

            return new StartableEndpoint(settings, containerComponent, featureActivator, transportInfrastructure, receiveComponent, criticalError, messageSession);
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

        MessageSession messageSession;
        ReceiveComponent receiveComponent;
        QueueBindings queueBindings;
        FeatureActivator featureActivator;
        TransportInfrastructure transportInfrastructure;
        CriticalError criticalError;
        SettingsHolder settings;
        PipelineComponent pipelineComponent;
        ContainerComponent containerComponent;
    }
}
