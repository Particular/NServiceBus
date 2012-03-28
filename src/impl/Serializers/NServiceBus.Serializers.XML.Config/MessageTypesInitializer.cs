namespace NServiceBus.Serializers.XML.Config
{
    using System.Linq;
    using NServiceBus.Config;
    using MessageInterfaces.MessageMapper.Reflection;
    using XML;

    /// <summary>
    /// Initializes the mapper and the serializer with the found message types
    /// </summary>
    public class MessageTypesInitializer:IWantToRunWhenConfigurationIsComplete
    {
        public MessageMapper Mapper { get; set; }
        public XmlMessageSerializer Serializer { get; set; }
        public void Run()
        {
            if (Mapper == null)
                return;

            var messageTypes = Configure.TypesToScan.Where(t => MessageConventionExtensions.IsMessageType(t)).ToList();

            Mapper.Initialize(messageTypes);
            Serializer.Initialize(messageTypes);
        }
    }
}