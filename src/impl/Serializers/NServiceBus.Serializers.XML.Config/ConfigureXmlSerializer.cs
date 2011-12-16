using NServiceBus.ObjectBuilder;
using System.Linq;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureXmlSerializer
    {
        /// <summary>
        /// Use XML serialization that supports interface-based messages.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure XmlSerializer(this Configure config)
        {
            var messageTypes = Configure.TypesToScan.Where(t => typeof (IMessage).IsAssignableFrom(t)).ToList();

            config.Configurer.ConfigureComponent<MessageInterfaces.MessageMapper.Reflection.MessageMapper>(ComponentCallModelEnum.Singleton);
            config.Configurer.ConfigureComponent<Serializers.XML.MessageSerializer>(ComponentCallModelEnum.Singleton)
                .ConfigureProperty(ms => ms.MessageTypes, messageTypes);

            return config;
        }

        /// <summary>
        /// Use XML serialization that supports interface-based messages.
        /// Optionally set the namespace to be used in the XML.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="nameSpace"></param>
        /// <returns></returns>
        public static Configure XmlSerializer(this Configure config, string nameSpace)
        {
            config.XmlSerializer();

            config.Configurer.ConfigureProperty<Serializers.XML.MessageSerializer>(x => x.Namespace, nameSpace);

            return config;
        }

        /// <summary>
        /// Use XML serialization that supports interface-based messages.
        /// Optionally set the namespace to be used in the XML.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sanitizeInput"></param>
        /// <returns></returns>
	public static Configure XmlSerializer(this Configure config, bool sanitizeInput)
	{
            config.XmlSerializer();

            config.Configurer.ConfigureProperty<Serializers.XML.MessageSerializer>(x => x.SanitizeInput, sanitizeInput);

            return config;
	}

        /// <summary>
        /// Use XML serialization that supports interface-based messages.
        /// Optionally set the namespace to be used in the XML.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sanitizeInput"></param>
        /// <returns></returns>
	public static Configure XmlSerializer(this Configure config, string nameSpace, bool sanitizeInput)
	{
            config.XmlSerializer();

            config.Configurer.ConfigureProperty<Serializers.XML.MessageSerializer>(x => x.Namespace, nameSpace);
            config.Configurer.ConfigureProperty<Serializers.XML.MessageSerializer>(x => x.SanitizeInput, sanitizeInput);

            return config;
	}
    }
}
