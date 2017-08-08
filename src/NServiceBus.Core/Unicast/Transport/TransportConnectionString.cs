namespace NServiceBus
{
    using System;
    using Transport;

    sealed class TransportConnectionString
    {
        TransportConnectionString()
        {
#if NET452
            GetValue = () => ReadConnectionStringFromAppConfig(DefaultConnectionStringName);
#else
            GetValue = () => null;
#endif
        }

        public TransportConnectionString(Func<string> func)
        {
            GetValue = func;
        }

#if NET452
        public TransportConnectionString(string name)
        {
            GetValue = () => ReadConnectionStringFromAppConfig(name);
        }

        static string ReadConnectionStringFromAppConfig(string connectionStringName)
        {
            var connectionStringSettings = System.Configuration.ConfigurationManager.ConnectionStrings[connectionStringName];

            return connectionStringSettings?.ConnectionString;
        }

        const string DefaultConnectionStringName = "NServiceBus/Transport";
#endif

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

#if NET452
        const string Message =
   @"Transport connection string has not been explicitly configured via ConnectionString method and no default connection has been was found in the app.config or web.config file for the {0} Transport.

To run NServiceBus with {0} Transport you need to specify the database connectionstring.
Here are examples of what is required:

  <connectionStrings>
    <add name=""NServiceBus/Transport"" connectionString=""{1}"" />
  </connectionStrings>

or

  busConfig.UseTransport<{0}>().ConnectionString(""{1}"");
";
#else
        const string Message = "Transport connection string has not been explicitly configured via ConnectionString method.";
#endif
    }
}