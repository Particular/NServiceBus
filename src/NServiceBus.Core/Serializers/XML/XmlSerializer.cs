namespace NServiceBus
{
    using System;
    using System.Linq;
    using MessageInterfaces;
    using Serialization;
    using Settings;

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
                var messageTypes = settings.GetAvailableTypes()
                    .Where(conventions.IsMessageType).ToList();

                var serializer = new XmlMessageSerializer(mapper, conventions);
                serializer.Initialize(messageTypes);
                return serializer;
            };
        }
    }
}