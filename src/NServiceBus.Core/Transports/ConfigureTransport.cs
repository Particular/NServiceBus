namespace NServiceBus.Transports
{
    using System;
    using Features;
    using Unicast.Transport;

    /// <summary>
    /// Base class for configuring <see cref="TransportDefinition"/> features.
    /// </summary>
    /// <typeparam name="T">The <see cref="TransportDefinition"/> to configure.</typeparam>
    public abstract class ConfigureTransport<T> : Feature where T : TransportDefinition, new()
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        protected ConfigureTransport()
        {
            Defaults(s => s.SetDefault<TransportConnectionString>(TransportConnectionString.Default));
        }

        /// <summary>
        /// <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var connectionString = context.Settings.Get<TransportConnectionString>().GetConnectionStringOrNull();

            if (connectionString == null && RequiresConnectionString)
            {
                throw new InvalidOperationException(String.Format(Message, GetConfigFileIfExists(), typeof(T).Name, ExampleConnectionStringForErrorMessage));
            }

            var selectedTransportDefinition = context.Settings.Get<TransportDefinition>();
            context.Container.RegisterSingleton(selectedTransportDefinition);

            Configure(context, connectionString);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigureTransport{T}"/>.
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