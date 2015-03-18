namespace NServiceBus.Transports
{
    using System;
    using Features;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using Unicast.Transport;

    /// <summary>
    /// Base class for configuring <see cref="TransportDefinition"/> features.
    /// </summary>
    public abstract class ConfigureTransport : Feature
    {
        /// <summary>
        ///  Initializes a new instance of <see cref="ConfigureTransport"/>.
        /// </summary>
        protected ConfigureTransport()
        {
            Defaults(s => s.SetDefault<TransportConnectionString>(TransportConnectionString.Default));

            Defaults(s => s.SetDefault("NServiceBus.LocalAddress", GetDefaultEndpointAddress(s)));

            Defaults(s =>
            {
                var localAddress = GetLocalAddress(s);
                if (!String.IsNullOrEmpty(localAddress) && !s.HasExplicitValue("NServiceBus.LocalAddress"))
                {
                    s.Set("NServiceBus.LocalAddress", localAddress);
                }
            });
        }

        /// <summary>
        /// <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var connectionString = context.Settings.Get<TransportConnectionString>().GetConnectionStringOrNull();
            var selectedTransportDefinition = context.Settings.Get<TransportDefinition>();

            if (connectionString == null && RequiresConnectionString)
            {
                throw new InvalidOperationException(String.Format(Message, GetConfigFileIfExists(), selectedTransportDefinition.GetType().Name, ExampleConnectionStringForErrorMessage));
            }

            context.Container.RegisterSingleton(selectedTransportDefinition);

            if (!context.Settings.Get<bool>("Endpoint.SendOnly"))
            {
                var receiveBehaviorFactory = GetReceiveBehaviorFactory(new ReceiveOptions(context.Settings));
                var registration = new ReceiveBehavior.Registration();
                registration.ContainerRegistration((b,s) => receiveBehaviorFactory(b));
                context.Pipeline.Register(registration);
                context.Container.RegisterSingleton(new TransportReceiveBehaviorDefinition(registration));
            }
            Configure(context, connectionString);
        }

        /// <summary>
        /// Creates a <see cref="RegisterStep"/> for receive behavior.
        /// </summary>
        /// <returns></returns>
        protected abstract Func<IBuilder, ReceiveBehavior> GetReceiveBehaviorFactory(ReceiveOptions receiveOptions);

        /// <summary>
        ///  Allows the transport to control the local address of the endpoint if needed
        /// </summary>
        /// <param name="settings">The current settings in read only mode</param>
        // ReSharper disable once UnusedParameter.Global
        protected virtual string GetLocalAddress(ReadOnlySettings settings)
        {
            return null;
        }

        /// <summary>
        /// Gives the chance to implementers to set themselves up.
        /// </summary>
        protected abstract void Configure(FeatureConfigurationContext context, string connectionString);

        /// <summary>
        /// Used by implementations to provide an example connection string that till be used for the possible exception thrown if the <see cref="RequiresConnectionString"/> requirement is not met.
        /// </summary>
        protected abstract string ExampleConnectionStringForErrorMessage { get; }

        /// <summary>
        /// Used by implementations to control if a connection string is necessary.
        /// </summary>
        /// <remarks>If this is true and a connection string is not returned by <see cref="TransportConnectionString.GetConnectionStringOrNull"/> then an exception will be thrown.</remarks>
        protected virtual bool RequiresConnectionString
        {
            get { return true; }
        }

        static string GetConfigFileIfExists()
        {
            return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile ?? "App.config";
        }

        static string GetDefaultEndpointAddress(ReadOnlySettings settings)
        {
            if (!settings.GetOrDefault<bool>("IndividualizeEndpointAddress"))
            {
                return settings.EndpointName();
            }

            if (!settings.HasSetting("EndpointInstanceDiscriminator"))
            {
                throw new Exception("No endpoint instance discriminator found. This value is usually provided by your transport so please make sure you're on the lastest version of your specific transport or set the discriminator using 'configuration.ScaleOut().UniqueQueuePerEndpointInstance(myDiscriminator)'");
            }

            return settings.EndpointName() + settings.Get<string>("EndpointInstanceDiscriminator");
        }


        const string Message =
            @"No default connection string found in your config file ({0}) for the {1} Transport.

To run NServiceBus with {1} Transport you need to specify the database connectionstring.
Here is an example of what is required:
  
  <connectionStrings>
    <add name=""NServiceBus/Transport"" connectionString=""{2}"" />
  </connectionStrings>";

    }

    class TransportReceiveBehaviorDefinition
    {
        readonly RegisterStep registration;

        public TransportReceiveBehaviorDefinition(RegisterStep registration)
        {
            this.registration = registration;
        }

        public RegisterStep Registration
        {
            get { return registration; }
        }


    }
}
