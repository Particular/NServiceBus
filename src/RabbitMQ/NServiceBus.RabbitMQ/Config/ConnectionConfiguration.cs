﻿using IHostConfiguration = EasyNetQ.IHostConfiguration;

namespace NServiceBus.Transports.RabbitMQ.Config
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using EasyNetQ;
    using NServiceBus.Settings;

    public class ConnectionConfiguration : IConnectionConfiguration
    {
        public const ushort DefaultHeartBeatInSeconds = 5;
        public const ushort DefaultPrefetchCount = 1;
        public const ushort DefaultPort = 5672;
        public static TimeSpan DefaultWaitTimeForConfirms = TimeSpan.FromSeconds(30);
        IDictionary<string, string> clientProperties = new Dictionary<string, string>();
        IEnumerable<IHostConfiguration> hosts= new List<IHostConfiguration>();

        public ushort Port { get; set; }
        public string VirtualHost { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public ushort RequestedHeartbeat { get; set; }
        public ushort PrefetchCount { get; set; }
        public ushort MaxRetries { get; set; }
        public bool UsePublisherConfirms { get; set; }
        public TimeSpan MaxWaitTimeForConfirms { get; set; }
        public TimeSpan RetryDelay { get; set; }
        public IDictionary<string, string> ClientProperties {
            get { return clientProperties; }
            private set { clientProperties = value; }
        }

        public IEnumerable<IHostConfiguration> Hosts {
            get { return hosts; }
            set { hosts = value; }
        }

        public ConnectionConfiguration()
        {
            // set default values
            Port = DefaultPort;
            VirtualHost = "/";
            UserName = "guest";
            Password = "guest";
            RequestedHeartbeat = DefaultHeartBeatInSeconds;
            PrefetchCount = DefaultPrefetchCount;
            UsePublisherConfirms = SettingsHolder.GetOrDefault<bool>("Endpoint.DurableMessages");
            MaxWaitTimeForConfirms = DefaultWaitTimeForConfirms;

            SetDefaultClientProperties();
        }

        private void SetDefaultClientProperties()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var applicationNameAndPath = Environment.GetCommandLineArgs()[0];
            var applicationName = Path.GetFileName(applicationNameAndPath);
            var applicationPath = Path.GetDirectoryName(applicationNameAndPath);
            var hostname = Environment.MachineName;

            clientProperties.Add("client_api", "NServiceBus - EasyNetQ");
            clientProperties.Add("nservicebus_version", version);
            clientProperties.Add("application", applicationName);
            clientProperties.Add("application_location", applicationPath);
            clientProperties.Add("machine_name", hostname);
            clientProperties.Add("user", UserName);
            clientProperties.Add("connected", DateTime.Now.ToString("G"));

        }

        public void Validate()
        {
            if (!Hosts.Any())
            {
                throw new Exception("Invalid connection string. 'host' value must be supplied. e.g: \"host=myserver\"");
            }
            foreach (var hostConfiguration in Hosts)
            {
                if (hostConfiguration.Port == 0)
                {
                    ((HostConfiguration)hostConfiguration).Port = Port;
                }
            }
        }
    }
}