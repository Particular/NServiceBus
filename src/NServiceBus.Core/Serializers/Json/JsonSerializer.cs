namespace NServiceBus
{
    using System;
    using System.Text;
    using MessageInterfaces;
    using Serialization;
    using Settings;

    /// <summary>
    /// Defines the capabilities of the JSON serializer.
    /// </summary>
    public class JsonSerializer : SerializationDefinition
    {
        /// <summary>
        /// Provides a factory method for building a message serializer.
        /// </summary>
        public override Func<IMessageMapper, IMessageSerializer> Configure(ReadOnlySettings settings)
        {
            var encoding = settings.GetOrDefault<Encoding>("Serialization.Json.Encoding") ?? Encoding.UTF8;
            return mapper => new JsonMessageSerializer(mapper, encoding);
        }
    }
}