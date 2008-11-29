using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.Unicast.Subscriptions.DB.Config;

namespace NServiceBus.Config
{
    public static class ConfigureDbSubscriptionStorage
    {
        public static ConfigDbSubscriptionStorage DbSubscriptionStorage(this NServiceBus.Config.Configure config)
        {
            ConfigDbSubscriptionStorage cfg = new ConfigDbSubscriptionStorage();
            cfg.Configure(config.builder);

            return cfg;
        }
    }
}
