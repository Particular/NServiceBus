namespace NServiceBus.Core.Tests.DataBus
{
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
            var deserializer = new DataBusDeserializer(new List<IDataBusSerializer> { new SystemJsonDataBusSerializer() });
            var somePropertyValue = "test";

            using (var stream = new MemoryStream())
            {
                jsonSerializer.Serialize(somePropertyValue, stream);
                stream.Position = 0;

                var deserializedProperty = deserializer.Deserialize(jsonSerializer.Name, typeof(string), stream);

                Assert.AreEqual(somePropertyValue, deserializedProperty);
            }
        }
    }
}