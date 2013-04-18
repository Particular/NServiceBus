using System.Collections.Generic;

namespace EasyNetQ
{
    using System;

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
        ushort MaxRetries { get; set; }
        TimeSpan DelayBetweenRetries { get; set; }
        bool UsePublisherConfirms { get; set; }
        TimeSpan MaxWaitTimeForConfirms { get; set; }
    }
}