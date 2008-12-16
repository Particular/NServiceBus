using System;
using System.Collections.Generic;
using System.Text;
using NServiceBus.Unicast.Config;

namespace NServiceBus
{
    public static class ConfigureUnicastBus
    {
        public static ConfigUnicastBus UnicastBus(this Configure config)
        {
            ConfigUnicastBus cfg = new ConfigUnicastBus();
            cfg.Configure(config);

            return cfg;
        } 
    }
}
