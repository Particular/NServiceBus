using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.Unicast.Subscriptions.Msmq.Config;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureMsmqSubscriptionStorage
    {
        /// <summary>
        /// Stores subscription data using MSMQ.
        /// If multiple machines need to share the same list of subscribers,
        /// you should not choose this option - prefer the DbSubscriptionStorage
        /// in that case.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ConfigMsmqSubscriptionStorage MsmqSubscriptionStorage(this Configure config)
        {
            ConfigMsmqSubscriptionStorage cfg = new ConfigMsmqSubscriptionStorage();
            cfg.Configure(config);

            return cfg;
        }
    }
}
