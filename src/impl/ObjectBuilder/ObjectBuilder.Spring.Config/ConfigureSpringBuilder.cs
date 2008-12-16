using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.ObjectBuilder.Common;
using NServiceBus.ObjectBuilder.Spring;
using NServiceBus.ObjectBuilder.Common.Config;
using ObjectBuilder;

namespace NServiceBus
{
    public static class ConfigureSpringBuilder
    {
        public static Configure SpringBuilder(this Configure config, params Action<IConfigureComponents>[] configActions)
        {
            ConfigureCommon.With<SpringObjectBuilder>(config);

            foreach (var a in configActions)
                a(config.Configurer);

            return config;
        }
    }
}
