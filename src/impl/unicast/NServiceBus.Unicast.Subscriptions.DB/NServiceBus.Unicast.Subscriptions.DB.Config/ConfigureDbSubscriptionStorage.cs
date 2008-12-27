using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.Unicast.Subscriptions.DB.Config;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureDbSubscriptionStorage
    {
        /// <summary>
        /// Use a database to store subscriber data.
        /// This allows multiple nodes to use the same subscription list
        /// and is the suggested implementation for production.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ConfigDbSubscriptionStorage DbSubscriptionStorage(this Configure config)
        {
            ConfigDbSubscriptionStorage cfg = new ConfigDbSubscriptionStorage();
            cfg.Configure(config);

            return cfg;
        }
    }
}
