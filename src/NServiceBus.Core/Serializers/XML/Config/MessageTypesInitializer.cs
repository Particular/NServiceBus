namespace NServiceBus.Serializers.XML.Config
{
    using System.Linq;
    using MessageInterfaces.MessageMapper.Reflection;
    using NServiceBus.Config;

    /// <summary>
    /// Initializes the mapper and the serializer with the found message types
    /// </summary>
    class MessageTypesInitializer : IWantToRunWhenConfigurationIsComplete
    {
        public MessageMapper Mapper { get; set; }
        public XmlMessageSerializer Serializer { get; set; }

        public void Run(Configure config)
        {
            if (Mapper == null)
            {
                return;
            }

            var messageTypes = config.TypesToScan.Where(config.Settings.Get<Conventions>().IsMessageType).ToList();

            Mapper.Initialize(messageTypes);

            if (Serializer != null)
            {
                Serializer.Initialize(messageTypes);
            }
        }
    }
}