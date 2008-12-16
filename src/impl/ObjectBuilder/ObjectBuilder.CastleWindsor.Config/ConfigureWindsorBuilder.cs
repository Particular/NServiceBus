using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ObjectBuilder;
using NServiceBus.ObjectBuilder.CastleWindsor;
using NServiceBus.ObjectBuilder.Common.Config;

namespace NServiceBus
{
    public static class ConfigureWindsorBuilder
    {
        public static Configure CastleWindsorBuilder(this Configure config, params Action<IConfigureComponents>[] configActions)
        {
            ConfigureCommon.With<WindsorObjectBuilder>(config);

            foreach (var a in configActions)
                a(config.Configurer);

            return config;
        }
    }
}
