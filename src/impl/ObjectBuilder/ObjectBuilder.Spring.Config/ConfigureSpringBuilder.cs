using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.ObjectBuilder.Common;
using NServiceBus.ObjectBuilder.Spring;
using NServiceBus.ObjectBuilder.Common.Config;
using ObjectBuilder;
using Spring.Context.Support;

namespace NServiceBus
{
    public static class ConfigureSpringBuilder
    {
        public static Configure SpringBuilder(this Configure config, params Action<IConfigureComponents>[] configActions)
        {
            ConfigureCommon.With(config, new SpringObjectBuilder(), configActions);

            return config;
        }

        public static Configure SpringBuilder(this Configure config, GenericApplicationContext container, params Action<IConfigureComponents>[] configActions)
        {
            ConfigureCommon.With(config, new SpringObjectBuilder(container), configActions);

            return config;
        }
    }
}
