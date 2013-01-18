namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Configuration;

    public class TransportConnectionString
    {
        public static void Override(Func<string> func)
        {
            GetValue = func;
        }
        public static string GetConnectionStringOrNull()
        {
            return GetValue();
        }

        static Func<string> GetValue = () =>
            {
                var connectionStringSettings = ConfigurationManager.ConnectionStrings["NServiceBus/Transport"];

                if (connectionStringSettings == null)
                {
                    return null;
                }

                return connectionStringSettings.ConnectionString;
            };
    }
}