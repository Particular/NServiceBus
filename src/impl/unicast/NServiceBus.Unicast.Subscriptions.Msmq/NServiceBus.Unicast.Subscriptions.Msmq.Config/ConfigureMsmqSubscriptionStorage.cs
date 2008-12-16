using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.Unicast.Subscriptions.Msmq.Config;

namespace NServiceBus
{
    public static class ConfigureMsmqSubscriptionStorage
    {
        public static ConfigMsmqSubscriptionStorage MsmqSubscriptionStorage(this Configure config)
        {
            ConfigMsmqSubscriptionStorage cfg = new ConfigMsmqSubscriptionStorage();
            cfg.Configure(config);

            return cfg;
        }
    }
}
