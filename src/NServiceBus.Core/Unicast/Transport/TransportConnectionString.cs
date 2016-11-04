namespace NServiceBus
{
    using System;
    using System.Configuration;

    /// <summary>
    /// Allows to get the configured transport connection string.
    /// </summary>
    public sealed class TransportConnectionString
    {
        TransportConnectionString()
        {
        }

        /// <summary>
        /// Creates new connection string configuration that returns value provided by a callback.
        /// </summary>
        /// <param name="func">Callback providing connection string value.</param>
        public TransportConnectionString(Func<string> func)
        {
            GetValue = func;
        }

        internal TransportConnectionString(string name)
        {
            GetValue = () => ReadConnectionString(name);
        }

        internal static TransportConnectionString Default => new TransportConnectionString();

        /// <summary>
        /// Returns the configured transport connection string.
        /// </summary>
        /// <returns></returns>
        public bool TryGetValue(out string connectionString)
        {
            connectionString = GetValue();
            return connectionString != null;
        }

        static string ReadConnectionString(string connectionStringName)
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringName];

            return connectionStringSettings?.ConnectionString;
        }

        Func<string> GetValue = () => ReadConnectionString(DefaultConnectionStringName);

        

        const string DefaultConnectionStringName = "NServiceBus/Transport";
    }
}