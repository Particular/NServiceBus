using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.Unicast.Transport.Msmq.Config;

namespace NServiceBus
{
    public static class ConfigureMsmqTransport
    {
        public static ConfigMsmqTransport MsmqTransport(this Configure config)
        {
            ConfigMsmqTransport cfg = new ConfigMsmqTransport();
            cfg.Configure(config);

            return cfg;
        }
    }
}
