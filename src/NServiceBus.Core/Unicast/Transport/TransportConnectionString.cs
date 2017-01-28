namespace NServiceBus
{
    using System;
    using System.Configuration;
    using Transport;

    /// <summary>
    /// Allows to get the configured transport connection string.
    /// </summary>
    public sealed class TransportConnectionString
    {
        internal TransportConnectionString(TransportDefinition transportDefinition)
        {
            this.transportDefinition = transportDefinition;
        }

        internal TransportConnectionString(Func<string> func, TransportDefinition transportDefinition)
            : this(transportDefinition)
        {
            GetValue = func;
        }


        internal TransportConnectionString(string name, TransportDefinition transportDefinition)
            : this(transportDefinition)
        {
            GetValue = () => ReadConnectionString(name);
        }
        
        /// <summary>
        /// Returns the configured transport connection string.
        /// </summary>
        public string GetConnectionStringOrRaiseError()
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
        TransportDefinition transportDefinition;

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