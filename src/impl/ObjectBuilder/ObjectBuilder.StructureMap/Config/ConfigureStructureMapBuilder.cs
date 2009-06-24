using NServiceBus.ObjectBuilder.Common.Config;
using NServiceBus.ObjectBuilder.StructureMap;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureStructureMapBuilder
    {
        /// <summary>
        /// Use StructureMap as your container.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure StructureMapBuilder(this Configure config)
        {
            ConfigureCommon.With(config, new StructureMapObjectBuilder());

            return config;
        }
    }
}

