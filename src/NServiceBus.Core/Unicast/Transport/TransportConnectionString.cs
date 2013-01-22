namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Configuration;

    public static class TransportConnectionString
    {
        public static void Override(Func<string> func)
        {
            GetValue = _ => func();
        }

        public static string GetConnectionStringOrNull(string connectionStringName = null)
        {
            return GetValue(connectionStringName ?? DefaultConnectionStringName);
        }

        static Func<string, string> GetValue = connectionStringName =>
            {
                var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringName];

                if (connectionStringSettings == null)
                {
                    return null;
                }

                return connectionStringSettings.ConnectionString;
            };

        public static string DefaultConnectionStringName = "NServiceBus/Transport";
    }
}