namespace NServiceBus.Core.Tests.DataBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.DataBus;
    using NUnit.Framework;

    [TestFixture]
    class DataBusDeserializerTests
    {
        [Test]
        public void Should_use_the_specified_serializer()
        {
            var jsonSerializer = new SystemJsonDataBusSerializer();
            var deserializer = new DataBusDeserializer(new List<IDataBusSerializer> { jsonSerializer });
            var somePropertyValue = "test";

            using (var stream = new MemoryStream())
            {
                jsonSerializer.Serialize(somePropertyValue, stream);
                stream.Position = 0;

                var deserializedProperty = deserializer.Deserialize(jsonSerializer.Name, typeof(string), stream);

                Assert.AreEqual(somePropertyValue, deserializedProperty);
            }
        }

        [Test]
        public void Should_throw_if_matching_serializer_not_found()
        {
            var jsonSerializer = new SystemJsonDataBusSerializer();
            var deserializer = new DataBusDeserializer(new List<IDataBusSerializer> { jsonSerializer });
            var somePropertyValue = "test";

            using (var stream = new MemoryStream())
            {
                jsonSerializer.Serialize(somePropertyValue, stream);
                stream.Position = 0;

                var ex = Assert.Throws<Exception>(() => deserializer.Deserialize("other-serializer-not-configured", typeof(string), stream));

                StringAssert.Contains("other-serializer-not-configured", ex.Message);
            }
        }

        [Test]
        public void Should_try_all_available_when_serializer_not_known()
        {
            var jsonSerializer = new SystemJsonDataBusSerializer();
#pragma warning disable CS0618
            var binarySerializer = new BinaryFormatterDataBusSerializer();
#pragma warning restore CS0618

            var deserializer = new DataBusDeserializer(new List<IDataBusSerializer> { jsonSerializer, binarySerializer });
            var somePropertyValue = "test";

            using (var stream = new MemoryStream())
            {
                binarySerializer.Serialize(somePropertyValue, stream);
                stream.Position = 0;

                var deserializedProperty = deserializer.Deserialize(null, typeof(string), stream);

                Assert.AreEqual(somePropertyValue, deserializedProperty);
            }
        }

        [Test]
        public void Should_throw_when_no_serializer_is_able_to_deserialize()
        {
            var jsonSerializer = new SystemJsonDataBusSerializer();
#pragma warning disable CS0618
            var binarySerializer = new BinaryFormatterDataBusSerializer();
#pragma warning restore CS0618

            var deserializer = new DataBusDeserializer(new List<IDataBusSerializer> { jsonSerializer });
            var somePropertyValue = "test";

            using (var stream = new MemoryStream())
            {
                binarySerializer.Serialize(somePropertyValue, stream);
                stream.Position = 0;

                var ex = Assert.Throws<Exception>(() => deserializer.Deserialize(null, typeof(string), stream));

                StringAssert.Contains(jsonSerializer.Name, ex.Message);
            }
        }
    }
}