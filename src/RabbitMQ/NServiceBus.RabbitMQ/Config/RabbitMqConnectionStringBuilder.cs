namespace NServiceBus.RabbitMQ.Config
{
    using System.Data.Common;
    using global::RabbitMQ.Client;

    public class RabbitMqConnectionStringBuilder : DbConnectionStringBuilder
    {

        public RabbitMqConnectionStringBuilder()
        {
        }

        public RabbitMqConnectionStringBuilder(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        public override bool ShouldSerialize(string keyword)
        {
            switch (keyword.ToLower())
            {
                case "host":
                    return true;
            }
            return false;
        }


        public string Host
        {
            get { return (string)this["host"]; }
            set { this["host"] = value; }
        }

        public ConnectionFactory BuildConnectionFactory()
        {
            return new ConnectionFactory
                {
                    HostName = Host
                };
        }
    }
}