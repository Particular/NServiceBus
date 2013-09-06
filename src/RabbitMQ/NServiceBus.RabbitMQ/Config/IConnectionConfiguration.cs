namespace NServiceBus.Transports.RabbitMQ.Config
{
    using System;
    using System.Collections.Generic;
    using EasyNetQ;

    public interface IConnectionConfiguration
    {
        ushort Port { get; }
        string VirtualHost { get; }
        string UserName { get; }
        string Password { get; }
        ushort RequestedHeartbeat { get; }
        ushort PrefetchCount { get; }
        IDictionary<string, string> ClientProperties { get; } 
        IEnumerable<IHostConfiguration> Hosts { get; }
        TimeSpan RetryDelay { get; set; }
        bool UsePublisherConfirms { get; set; }
        TimeSpan MaxWaitTimeForConfirms { get; set; }
    }
}