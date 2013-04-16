namespace NServiceBus.Transports.RabbitMQ
{
    using System;
    using EasyNetQ;

    public static class ConnectionConfigurationExtensions
    {
        public static void OverrideClientProperties(this IConnectionConfiguration connectionConfiguration) {
            // EasyNetQ hardcodes this stuff in a private constructor - overriding for NSB
            connectionConfiguration.ClientProperties["client_api"] = "NServiceBus - EasyNetQ";
            connectionConfiguration.ClientProperties["connected"] = DateTime.Now.ToString("G");
            var version = connectionConfiguration.ClientProperties["easynetq_version"];
            connectionConfiguration.ClientProperties.Remove("easynetq_version");
            connectionConfiguration.ClientProperties.Add("nservicebus_version", version);
        }

    }
}