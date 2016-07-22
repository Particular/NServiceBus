namespace NServiceBus
{
    using System;
    using System.Linq;
    using MessageInterfaces;
    using Serialization;
    using Settings;
    using Unicast.Messages;

    /// <summary>
    /// Defines the capabilities of the XML serializer.
    /// </summary>
    public class XmlSerializer : SerializationDefinition
    {
        /// <summary>
        /// Provides a factory method for building a message serializer.
        /// </summary>
        public override Func<IMessageMapper, IMessageSerializer> Configure(ReadOnlySettings settings)
        {
            return mapper =>
            {
                var conventions = settings.Get<Conventions>();
                var serializer = new XmlMessageSerializer(mapper, conventions);

                string customNamespace;
                if (settings.TryGet(CustomNamespaceConfigurationKey, out customNamespace))
                {
                    serializer.Namespace = customNamespace;
                }

                bool skipWrappingRawXml;
                if (settings.TryGet(SkipWrappingRawXml, out skipWrappingRawXml))
                {
                    serializer.SkipWrappingRawXml = skipWrappingRawXml;
                }

                bool sanitizeInput;
                if (settings.TryGet(SanitizeInput, out sanitizeInput))
                {
                    serializer.SanitizeInput = sanitizeInput;
                }

                var registry = settings.Get<MessageMetadataRegistry>();
                var messageTypes = registry.GetAllMessages().Select(m => m.MessageType);

                serializer.Initialize(messageTypes);
                return serializer;
            };
        }

        internal const string CustomNamespaceConfigurationKey = "XmlSerializer.CustomNamespace";
        internal const string SkipWrappingRawXml = "XmlSerializer.SkipWrappingRawXml";
        internal const string SanitizeInput = "XmlSerializer.SkipWrappingRawXml";
    }
}