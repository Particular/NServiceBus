using System;
using NServiceBus.ObjectBuilder;
using NServiceBus.ObjectBuilder.Common.Config;
using NServiceBus.ObjectBuilder.StructureMap;

namespace NServiceBus
{
    public static class ConfigureStructureMapBuilder
    {
        public static Configure StructureMapBuilder(this Configure config, params Action<IConfigureComponents>[] configActions)
        {
            ConfigureCommon.With(config, new StructureMapObjectBuilder(), configActions);

            return config;
        }
    }
}

