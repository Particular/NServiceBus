using System;
using NServiceBus.ObjectBuilder;
using NServiceBus.ObjectBuilder.Common.Config;
using NServiceBus.ObjectBuilder.StructureMap;
using StructureMap;

namespace NServiceBus
{
    public static class ConfigureStructureMapBuilder
    {
        public static Configure StructureMapBuilder(this Configure config, params Action<IConfigureComponents>[] configActions)
        {
            ConfigureCommon.With(config, new StructureMapObjectBuilder(), configActions);

            return config;
        }

        public static Configure CastleWindsorBuilder(this Configure config, IContainer container, params Action<IConfigureComponents>[] configActions)
        {
            ConfigureCommon.With(config, new StructureMapObjectBuilder(container), configActions);

            return config;
        }


    }
}

