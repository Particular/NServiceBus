namespace NServiceBus
{
    using ObjectBuilder.Common.Config;
    using ObjectBuilder.StructureMap;
    using StructureMap;

    /// <summary>
    /// Contains extension methods to <see cref="Configure"/>.
    /// </summary>
    public static class ConfigureStructureMapBuilder
    {
        /// <summary>
        /// Use StructureMap as your container.
        /// </summary>
        public static Configure StructureMapBuilder(this Configure config)
        {
            ConfigureCommon.With(config, new StructureMapObjectBuilder());

            return config;
        }

        /// <summary>
        /// Use StructureMap as your container passing in a pre-configured container to be used by nServiceBus.
        /// </summary>
        public static Configure StructureMapBuilder(this Configure config, IContainer container)
        {
            ConfigureCommon.With(config, new StructureMapObjectBuilder(container));

            return config;
        }
    }
}

