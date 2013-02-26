namespace NServiceBus.Transports.RabbitMQ.Config
{
    using System;
    using System.Data.Common;
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

            if (ContainsKey("maxretries"))
                settings.MaxRetries = int.Parse(this["maxretries"] as string);

            if (ContainsKey("retry_delay"))
                settings.DelayBetweenRetries = TimeSpan.Parse(this["retry_delay"] as string);

            return settings;
        }


        const ushort DefaultHeartBeatInSeconds = 5;
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