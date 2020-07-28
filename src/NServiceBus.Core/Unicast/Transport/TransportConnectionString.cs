namespace NServiceBus
{
    using System;
    using Transport;

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
}