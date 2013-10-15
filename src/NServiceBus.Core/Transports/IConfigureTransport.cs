namespace NServiceBus.Transports
{
    using System;
    using Features;
    using Settings;
    using Unicast.Transport;

    /// <summary>
    /// Configures the given transport using the default settings
    /// </summary>
    public interface IConfigureTransport
    {
        void Configure(Configure config);
    }


    /// <summary>
    /// The generic counterpart to IConfigureTransports
    /// </summary>
    public interface IConfigureTransport<T> : IConfigureTransport where T : TransportDefinition { }

    public abstract class ConfigureTransport<T> : Feature, IConfigureTransport<T> where T : TransportDefinition
    {
        public void Configure(Configure config)
        {
            var connectionString = TransportConnectionString.GetConnectionStringOrNull();

            if (connectionString == null && RequiresConnectionString)
            {
                throw new InvalidOperationException(String.Format(Message, GetConfigFileIfExists(), typeof(T).Name, ExampleConnectionStringForErrorMessage));
            }

            SettingsHolder.Set("NServiceBus.Transport.ConnectionString", connectionString);
            SettingsHolder.Set("NServiceBus.Transport.SelectedTransport", Activator.CreateInstance<T>());

            InternalConfigure(config);
        }

        protected abstract void InternalConfigure(Configure config);

        protected abstract string ExampleConnectionStringForErrorMessage { get; }

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