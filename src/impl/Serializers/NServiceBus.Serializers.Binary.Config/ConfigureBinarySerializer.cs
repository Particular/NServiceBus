using NServiceBus.ObjectBuilder;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureBinarySerializer
    {
        /// <summary>
        /// Use binary serialization.
        /// Note that this does not support interface-based messages.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure BinarySerializer(this Configure config)
        {
            config.Configurer.ConfigureComponent<Serializers.Binary.SimpleMessageMapper>(ComponentCallModelEnum.Singleton);
            config.Configurer.ConfigureComponent(typeof(Serializers.Binary.MessageSerializer), ComponentCallModelEnum.Singleton);

            return config;
        }
    }
}
