namespace NServiceBus.RabbitMq.Config
{
    using System.Configuration;
    using System.Data.Common;
    using global::RabbitMQ.Client;

    public class RabbitMqConnectionStringBuilder : DbConnectionStringBuilder
    {

        public RabbitMqConnectionStringBuilder(string connectionString)
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

            if (ContainsKey("requestedHeartbeat"))
                factory.RequestedHeartbeat = ushort.Parse(this["requestedHeartbeat"] as string);


            return factory;
        }
    }
}