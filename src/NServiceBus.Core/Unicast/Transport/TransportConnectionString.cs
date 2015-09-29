namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Configuration;

    class TransportConnectionString
    {
        protected TransportConnectionString()
        {
        }

        public string GetConnectionStringOrNull()
        {
            return GetValue();
        }

        Func<string> GetValue = () => ReadConnectionString(DefaultConnectionStringName);


        static string ReadConnectionString(string connectionStringName)
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringName];

            return connectionStringSettings?.ConnectionString;
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

        const string DefaultConnectionStringName = "NServiceBus/Transport";

    }
}