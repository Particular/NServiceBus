namespace NServiceBus
{
    using System;
    using Transport;

#if NET452
    using Logging;
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
                Log.WarnFormat("A connection string named '{0}' was found. Using named connection strings is discouraged. Instead, load the connection string in your code and pass the value to EndpointConfiguration.UseTransport().ConnectionString(connectionString).", connectionStringName);
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
                throw new InvalidOperationException(string.Format(Message, transportDefinition.GetType().Name, transportDefinition.ExampleConnectionStringForErrorMessage));
            }
            return connectionString;
        }

        static ILog Log => LogManager.GetLogger<TransportExtensions>();

        Func<string> GetValue;

        const string Message =
@"Transport connection string has not been explicitly configured via 'ConnectionString' method and no default connection was found in the app.config or web.config file for the {0} Transport.

To run NServiceBus with {0} Transport you need to specify the database connectionstring.
Here are examples of what is required:

  <connectionStrings>
    <add name=""NServiceBus/Transport"" connectionString=""{1}"" />
  </connectionStrings>

or

  busConfig.UseTransport<{0}>().ConnectionString(""{1}"");
";
    }
#endif

#if NETSTANDARD2_0
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
                throw new InvalidOperationException(string.Format(Message, transportDefinition.GetType().Name, transportDefinition.ExampleConnectionStringForErrorMessage));
            }
            return connectionString;
        }

        Func<string> GetValue;

        const string Message = "Transport connection string has not been explicitly configured via 'ConnectionString' method.";
    }
#endif
}