namespace NServiceBus.Serializers.XML.Config
{
    using Serialization;

    /// <summary>
    /// Makes sure that we default to XML if users hasn't requested another serializer
    /// </summary>
    public class SetXmlSerializerAsDefault : INeedInitialization
    {
        internal static bool UseXmlSerializer;

        void INeedInitialization.Init()
        {
            if (!Configure.Instance.Configurer.HasComponent<IMessageSerializer>() && UseXmlSerializer)
                Configure.Instance.XmlSerializer();
        }
    }
}