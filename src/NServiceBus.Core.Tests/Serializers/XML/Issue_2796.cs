namespace NServiceBus.Core.Tests.Serializers.XML
{
    using System;
    using System.IO;
    using NServiceBus.Serializers.XML.Test;
    using NUnit.Framework;

    [TestFixture]
    public class Issue_2796
    {
        [Test]
        public void Object_property_with_primitive_or_struct_value_should_serialize_correctly()
        {
            var serializer = SerializerFactory.Create<SerializedPair>();
            var message = new SerializedPair
            {
                Key = "AddressId",
                Value = new Guid("{ebdeeb33-baa7-4100-b1aa-eb4d6816fd3d}")
            };

            object[] messageDeserialized;
            using (Stream stream = new MemoryStream())
            {
                serializer.Serialize(message, stream);

                stream.Position = 0;

                messageDeserialized = serializer.Deserialize(stream, new[] { message.GetType() });
            }

            Assert.AreEqual(message.Key, ((SerializedPair)messageDeserialized[0]).Key);
            Assert.AreEqual(message.Value, ((SerializedPair)messageDeserialized[0]).Value);
        }

        public class SerializedPair
        {
            public string Key { get; set; }
            public object Value { get; set; }
        }
    }
}