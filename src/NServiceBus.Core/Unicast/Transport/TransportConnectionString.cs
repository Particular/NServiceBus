namespace NServiceBus
{
    using System;
    using System.Configuration;
    using Transport;

    sealed class TransportConnectionString
    {
        TransportConnectionString()
        {
        }

        public TransportConnectionString(Func<string> func)
        {
            GetValue = func;
        }


        public TransportConnectionString(string name)
        {
            GetValue = () => ReadConnectionString(name);
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

        static string ReadConnectionString(string connectionStringName)
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringName];

            return connectionStringSettings?.ConnectionString;
        }

        Func<string> GetValue = () => ReadConnectionString(DefaultConnectionStringName);

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

        const string DefaultConnectionStringName = "NServiceBus/Transport";
    }
}