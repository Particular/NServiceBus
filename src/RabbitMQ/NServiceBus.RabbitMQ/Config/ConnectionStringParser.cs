using IHostConfiguration = EasyNetQ.IHostConfiguration;

namespace NServiceBus.Transports.RabbitMQ.Config
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using EasyNetQ;

    public class ConnectionStringParser : DbConnectionStringBuilder, IConnectionStringParser
    {
        ConnectionConfiguration connectionConfiguration;
        public IConnectionConfiguration Parse(string connectionString)
        {
            ConnectionString = connectionString;

            try
            {
                connectionConfiguration = new ConnectionConfiguration();

                if (ContainsKey("host"))
                    connectionConfiguration.Hosts = ParseHosts(this["host"] as string);

                if (ContainsKey("virtualHost"))
                    connectionConfiguration.VirtualHost = this["virtualHost"] as string;

                if (ContainsKey("username"))
                    connectionConfiguration.UserName = this["username"] as string;

                if (ContainsKey("password"))
                    connectionConfiguration.Password = this["password"] as string;

                if (ContainsKey("port"))
                    connectionConfiguration.Port = ushort.Parse(this["port"] as string);

                if( ContainsKey("requestedHeartbeat")) 
                    connectionConfiguration.RequestedHeartbeat = ushort.Parse(this["requestedHeartbeat"] as string);

                if( ContainsKey("prefetchCount")) 
                    connectionConfiguration.PrefetchCount = ushort.Parse(this["prefetchCount"] as string);

                if (ContainsKey("maxRetries"))
                    connectionConfiguration.MaxRetries = ushort.Parse(this["maxRetries"] as string);

                if (ContainsKey("retryDelay"))
                    connectionConfiguration.DelayBetweenRetries = TimeSpan.Parse(this["retryDelay"] as string);

                if (ContainsKey("usePublisherConfirms"))
                    connectionConfiguration.UsePublisherConfirms = bool.Parse(this["usePublisherConfirms"] as string);

                if (ContainsKey("maxWaitTimeForConfirms"))
                    connectionConfiguration.MaxWaitTimeForConfirms = TimeSpan.Parse(this["maxWaitTimeForConfirms"] as string);

                connectionConfiguration.Validate();
                return connectionConfiguration;
            }
            catch (Exception parseException)
            {
                throw new Exception(string.Format("Connection String parsing exception {0}", parseException.Message));
            }
        }

        IEnumerable<IHostConfiguration> ParseHosts(string hostsConnectionString) {
            var hosts = hostsConnectionString.Split(',');
            foreach (var hostAndPort in hosts) {
                var parts = hostAndPort.Split(':');
                var host = parts[0];
                var port = connectionConfiguration.Port;
                if (parts.Length == 2) {
                    port = ushort.Parse(parts[1]);
                }
                yield return new HostConfiguration{Host = host, Port = port};
            }
        }
    }
}