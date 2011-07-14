using NServiceBus.ObjectBuilder.Common.Config;
using NServiceBus.ObjectBuilder.StructureMap;
using StructureMap;

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

        /// <summary>
        /// Use StructureMap as your container passing in a preconfigured container to be used by nServiceBus.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public static Configure StructureMapBuilder(this Configure config, IContainer container)
        {
            ConfigureCommon.With(config, new StructureMapObjectBuilder(container));

            return config;
        }
    }
}

