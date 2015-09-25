namespace NServiceBus.Transports
{
    using System;
    using Features;
    using NServiceBus.Unicast.Transport;

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

            Defaults(s =>
            {
                const string translationKey = "LogicalToTransportAddressTranslation";
                Func<LogicalAddress, string, string> emptyTranslation = (logicalAddress, defaultTranslation) => defaultTranslation;
                s.SetDefault(translationKey, emptyTranslation);
                var translation = s.Get<Func<LogicalAddress, string, string>>(translationKey);
                s.Set<LogicalToTransportAddressTranslation>(new LogicalToTransportAddressTranslation(s.Get<TransportDefinition>(), translation));
            });

            Defaults(s =>
            {
                var transport = s.Get<LogicalToTransportAddressTranslation>();
                var defaultTransportAddress = transport.Translate(s.RootLogicalAddress());
                s.SetDefault("NServiceBus.LocalAddress", defaultTransportAddress);
            });
        }

        /// <summary>
        /// <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var selectedTransportDefinition = context.Settings.Get<TransportDefinition>();
            context.Settings.Get<QueueBindings>().BindReceiving(context.Settings.LocalAddress());

            var connectionString = context.Settings.Get<TransportConnectionString>().GetConnectionStringOrNull();
            if (connectionString == null && RequiresConnectionString)
            {
                throw new InvalidOperationException(String.Format(Message, GetConfigFileIfExists(), selectedTransportDefinition.GetType().Name, ExampleConnectionStringForErrorMessage));
            }

            context.Container.RegisterSingleton(selectedTransportDefinition);

          
            Configure(context, connectionString);
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

        const string Message =
            @"No default connection string found in your config file ({0}) for the {1} Transport.

To run NServiceBus with {1} Transport you need to specify the database connectionstring.
Here is an example of what is required:
  
  <connectionStrings>
    <add name=""NServiceBus/Transport"" connectionString=""{2}"" />
  </connectionStrings>";

    }
}
