namespace NServiceBus
{
    using ObjectBuilder.Autofac;
    using ObjectBuilder.Common.Config;

    /// <summary>
    /// Configuration extension for the default builder
    /// </summary>
    public static class ConfigureDefaultBuilder
    {

        /// <summary>
        /// Uses the default container merged into NServiceBus.Core.dll.
        /// In this version, the container is the Spring Framework.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure DefaultBuilder(this Configure config)
        {
            ConfigureCommon.With(config, new BuiltInObjectBuilder());

            return config;
        }

    }
}
