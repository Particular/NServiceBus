namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Configuration;
    using Config;
    using Config.ConfigurationSource;
    using Unicast.Queuing.Azure.ServiceBus;

    public class AzureServiceBusOverrides : IProvideConfiguration<AzureServiceBusQueueConfig>
    {
        public AzureServiceBusQueueConfig GetConfiguration()
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureServiceBus.ConnectionString");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ConfigurationErrorsException("No connection string for AzureServiceBus found, please add env variable: 'AzureServiceBus.ConnectionString'");
            }

            return new AzureServiceBusQueueConfig
                {
                    ConnectionString = connectionString,
                    ServerWaitTime = AzureServicebusDefaults.DefaultServerWaitTime
                };
        }
    }
}