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
            pipelineComponent.InitializeBuilder(containerComponent.Builder);

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

            var messageSession = new MessageSession(pipelineComponent.CreateRootContext(containerComponent.Builder));

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