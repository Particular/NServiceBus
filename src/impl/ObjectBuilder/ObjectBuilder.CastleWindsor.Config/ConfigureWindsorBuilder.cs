using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ObjectBuilder;
using NServiceBus.ObjectBuilder.CastleWindsor;
using NServiceBus.ObjectBuilder.Common.Config;
using Castle.Windsor;

namespace NServiceBus
{
    public static class ConfigureWindsorBuilder
    {
        public static Configure CastleWindsorBuilder(this Configure config, params Action<IConfigureComponents>[] configActions)
        {
            ConfigureCommon.With(config, new WindsorObjectBuilder(), configActions);

            return config;
        }

        public static Configure CastleWindsorBuilder(this Configure config, IWindsorContainer container, params Action<IConfigureComponents>[] configActions)
        {
            ConfigureCommon.With(config, new WindsorObjectBuilder(container), configActions);

            return config;
        }
        
    }
}
