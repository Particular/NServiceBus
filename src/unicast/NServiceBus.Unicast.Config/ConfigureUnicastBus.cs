using System;
using System.Collections.Generic;
using System.Text;
using NServiceBus.Unicast.Config;

namespace NServiceBus.Config
{
    public static class ConfigureUnicastBus
    {
        public static ConfigUnicastBus UnicastBus(this NServiceBus.Config.Configure config)
        {
            ConfigUnicastBus cfg = new ConfigUnicastBus();
            cfg.Configure(config.builder);

            return cfg;
        } 
    }
}
