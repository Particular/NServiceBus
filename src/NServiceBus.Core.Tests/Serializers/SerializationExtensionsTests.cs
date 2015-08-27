namespace NServiceBus.Core.Tests.Serializers
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Serialization;
    using NUnit.Framework;

    [TestFixture]
    public class SerializationExtensionsTests
    {
        [Test]
        public void Can_add_deserializers_in_addition_to_default_one()
        {
            var config = new BusConfiguration();
            config.AddDeserializer<JsonSerializer>();

            var deserializers = config.Settings.Get<HashSet<SerializationDefinition>>("AdditionalDeserializers");
            Assert.AreEqual(1, deserializers.Count);
            Assert.IsInstanceOf<JsonSerializer>(deserializers.First());
        }

        [Test]
        public void Deserializer_is_only_added_once()
        {
            var config = new BusConfiguration();
            config.AddDeserializer<JsonSerializer>();
            config.AddDeserializer<JsonSerializer>();

            var deserializers = config.Settings.Get<HashSet<SerializationDefinition>>("AdditionalDeserializers");
            Assert.AreEqual(1, deserializers.Count);
        }
    }
}