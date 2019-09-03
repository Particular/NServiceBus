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
        internal PreparedEndpoint()
        {
            MessageSession = new UninitializedMessageSession();
        }

        /// <summary>
        /// The message session (can only be used once the endpoint have been started).
        /// </summary>
        public IMessageSession MessageSession { get; private set; }

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

            MessageSession = new MessageSession(pipelineComponent.CreateRootContext(containerComponent.Builder));

            return new StartableEndpoint(settings, containerComponent, featureActivator, transportInfrastructure, receiveComponent, criticalError, MessageSession);
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

        ReceiveComponent receiveComponent;
        QueueBindings queueBindings;
        FeatureActivator featureActivator;
        TransportInfrastructure transportInfrastructure;
        CriticalError criticalError;
        SettingsHolder settings;
        PipelineComponent pipelineComponent;
        ContainerComponent containerComponent;

        class UninitializedMessageSession : IMessageSession
        {
            public Task Publish(object message, PublishOptions options)
            {
                throw new InvalidOperationException(ExceptionMessage);
            }

            public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
            {
                throw new InvalidOperationException(ExceptionMessage);
            }

            public Task Send(object message, SendOptions options)
            {
                throw new InvalidOperationException(ExceptionMessage);
            }

            public Task Send<T>(Action<T> messageConstructor, SendOptions options)
            {
                throw new InvalidOperationException(ExceptionMessage);
            }

            public Task Subscribe(Type eventType, SubscribeOptions options)
            {
                throw new InvalidOperationException(ExceptionMessage);
            }

            public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
            {
                throw new InvalidOperationException(ExceptionMessage);
            }

            static string ExceptionMessage = "The message session can only be used after the endpoint is started.";
        }
    }
}
