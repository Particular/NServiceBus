namespace NServiceBus
{
    using System;
    using Transport;

#if NETFRAMEWORK
    sealed class TransportConnectionString
    {
        TransportConnectionString()
        {
            GetValue = () => ReadConnectionStringFromAppConfig(DefaultConnectionStringName);
        }

        public TransportConnectionString(Func<string> func)
        {
            GetValue = func;
        }

        public TransportConnectionString(string name)
        {
            GetValue = () => ReadConnectionStringFromAppConfig(name);
        }

        static string ReadConnectionStringFromAppConfig(string connectionStringName)
        {
            var connectionStringSettings = System.Configuration.ConfigurationManager.ConnectionStrings[connectionStringName];

            if (connectionStringSettings?.ConnectionString != null)
            {
                logger.WarnFormat("A connection string named '{0}' was found. Using named connection strings is discouraged. Instead, load the connection string in your code and pass the value to EndpointConfiguration.UseTransport().ConnectionString(connectionString).", connectionStringName);
            }

            return connectionStringSettings?.ConnectionString;
        }

        const string DefaultConnectionStringName = "NServiceBus/Transport";

        public static TransportConnectionString Default => new TransportConnectionString();

        public string GetConnectionStringOrRaiseError(TransportDefinition transportDefinition)
        {
            var connectionString = GetValue();

            if (connectionString == null && transportDefinition.RequiresConnectionString)
            {
                throw new InvalidOperationException(string.Format(message, transportDefinition.GetType().Name, transportDefinition.ExampleConnectionStringForErrorMessage));
            }

            return connectionString;
        }

        static Logging.ILog logger = Logging.LogManager.GetLogger<TransportExtensions>();

        Func<string> GetValue;

        const string message =
@"Transport connection string has not been explicitly configured via 'ConnectionString' method, and no connection string was found in the app.config or web.config file.

Here are examples of what is required:

  endpointConfig.UseTransport<{0}>().ConnectionString(""{1}"");

or

  <connectionStrings>
    <add name=""NServiceBus/Transport"" connectionString=""{1}"" />
  </connectionStrings>
";
    }
#endif

#if NETSTANDARD || NETCOREAPP2_1
    sealed class TransportConnectionString
    {
        TransportConnectionString()
        {
            GetValue = () => null;
        }

        public TransportConnectionString(Func<string> func)
        {
            GetValue = func;
        }

        public static TransportConnectionString Default => new TransportConnectionString();

        public string GetConnectionStringOrRaiseError(TransportDefinition transportDefinition)
        {
            var connectionString = GetValue();

            if (connectionString == null && transportDefinition.RequiresConnectionString)
            {
                throw new InvalidOperationException(string.Format(message, transportDefinition.GetType().Name, transportDefinition.ExampleConnectionStringForErrorMessage));
            }

            return connectionString;
        }

        Func<string> GetValue;

        const string message = "Transport connection string has not been explicitly configured via 'ConnectionString' method. Here is an example of what is required: endpointConfig.UseTransport<{0}>().ConnectionString(\"{1}\");";
    }
#endif
}