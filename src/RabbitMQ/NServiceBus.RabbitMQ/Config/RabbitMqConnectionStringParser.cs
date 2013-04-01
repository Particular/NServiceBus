namespace NServiceBus.Transports.RabbitMQ.Config
{
    using System;
    using System.Data.Common;
    using Settings;
    using global::RabbitMQ.Client;

    public class RabbitMqConnectionStringParser : DbConnectionStringBuilder
    {

        public RabbitMqConnectionStringParser(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        public ConnectionFactory BuildConnectionFactory()
        {
            var factory = new ConnectionFactory();

            if (ContainsKey("host"))
                factory.HostName = this["host"] as string;

            if (ContainsKey("virtualHost"))
                factory.VirtualHost = this["virtualHost"] as string;

            if (ContainsKey("username"))
                factory.UserName = this["username"] as string;

            if (ContainsKey("password"))
                factory.Password = this["password"] as string;

            if (ContainsKey("port"))
                factory.Port = int.Parse(this["port"] as string);

            factory.RequestedHeartbeat = ContainsKey("requestedHeartbeat") ? ushort.Parse(this["requestedHeartbeat"] as string) : DefaultHeartBeatInSeconds;

            return factory;
        }


        public ConnectionRetrySettings BuildConnectionRetrySettings()
        {
            var settings = new ConnectionRetrySettings();

            if (ContainsKey("maxRetries"))
                settings.MaxRetries = int.Parse(this["maxRetries"] as string);

            if (ContainsKey("retryDelay"))
                settings.DelayBetweenRetries = TimeSpan.Parse(this["retryDelay"] as string);

            return settings;
        }

        public ushort GetPrefetchCount()
        {
            return ContainsKey("prefetchCount") ? ushort.Parse(this["prefetchCount"] as string) : DefaultPrefetchCount;
        }

        public TimeSpan GetMaxWaitTimeForConfirms()
        {
            return ContainsKey("maxWaitTimeForConfirms") ? TimeSpan.Parse(this["maxWaitTimeForConfirms"] as string) : DefaultWaitTimeForConfirms;
        }

        public bool UsePublisherConfirms()
        {
            var defaultValue = SettingsHolder.Get<bool>("Endpoint.DurableMessages");

            if (!ContainsKey("usePublisherConfirms"))
            {
                return defaultValue;
            }

            return bool.Parse(this["usePublisherConfirms"] as string);
        }


        const ushort DefaultHeartBeatInSeconds = 5;
        const ushort DefaultPrefetchCount = 1;
        static TimeSpan DefaultWaitTimeForConfirms = TimeSpan.FromSeconds(30);
    }

    public class ConnectionRetrySettings
    {
        public ConnectionRetrySettings()
        {
            MaxRetries = 6;
            DelayBetweenRetries = TimeSpan.FromSeconds(10);
        }

        public int MaxRetries { get; set; }

        public TimeSpan DelayBetweenRetries { get; set; }
    }
}