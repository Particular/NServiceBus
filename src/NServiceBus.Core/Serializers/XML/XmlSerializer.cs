namespace NServiceBus
{
    using System;
    using System.Linq;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.Settings;
    using Serialization;

    /// <summary>
    /// Defines the capabilities of the XML serializer.
    /// </summary>
    public class XmlSerializer : SerializationDefinition
    {
        /// <summary>
        /// Provides a factory method for building a message serializer.
        /// </summary>
        protected internal override Func<IMessageMapper, IMessageSerializer> Configure(ReadOnlySettings settings)
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