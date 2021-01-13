namespace NServiceBus.Serializers.XML.Test
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using NUnit.Framework;


    public class MessageWithArray
    {
        public Guid SagaId { get; set; }
        public int[] SomeInts { get; set; }

        public MessageWithArray(Guid sagaId, int[] someInts)
        {
            SagaId = sagaId;
            SomeInts = someInts;
        }
    }


    public class MessageWithArrayAndNoDefaultCtor
    {
        public Guid SagaId { get; set; }
        public string[] SomeWords { get; set; }
    }


    public class MessageWithNullableArray
    {
        public Guid SagaId { get; set; }
        public int?[] SomeInts { get; set; }
    }

    [TestFixture]
    public class SerializingArrayTests
    {
        [Test]
        public void CanDeserializeXmlWithWhitespace()
        {
            var xml =
              @"<?xml version=""1.0"" encoding=""utf-8""?>
<Messages xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
    <MessageWithArray>
        <SagaId>6bddc475-22a5-433b-a3ed-9edf00e8e353</SagaId>
        <SomeInts>
            <Int32>1405154</Int32>
        </SomeInts>
    </MessageWithArray>
</Messages>";

            var data = Encoding.UTF8.GetBytes(xml);

            var serializer = SerializerFactory.Create<MessageWithArray>();

            var messages = serializer.Deserialize(new MemoryStream(data));

            Assert.NotNull(messages);
            Assert.That(messages, Has.Length.EqualTo(1));

            Assert.That(messages[0], Is.TypeOf(typeof(MessageWithArray)));
            var m = (MessageWithArray)messages[0];

            Assert.IsNotNull(m.SomeInts);
            Assert.That(m.SomeInts, Has.Length.EqualTo(1));
        }

        [Test]
        public void CanSerializeAndBack()
        {
            var message = new MessageWithArray(Guid.NewGuid(), new[] { 1234, 5323 });

            var result = ExecuteSerializer.ForMessage<MessageWithArray>(message);

            Assert.IsNotNull(result.SomeInts);
            Assert.That(result.SomeInts, Has.Length.EqualTo(2));
            Assert.AreEqual(1234, result.SomeInts[0]);
            Assert.AreEqual(5323, result.SomeInts[1]);
        }

        [Test]
        public void CanSerializeMessageWithNullArray()
        {
            var message = new MessageWithArrayAndNoDefaultCtor
            {
                SomeWords = null
            };

            var result = ExecuteSerializer.ForMessage<MessageWithArrayAndNoDefaultCtor>(message);

            Assert.IsNull(result.SomeWords);
        }

        [Test]
        public void CanSerializeMessageWithEmptyArray()
        {
            var message = new MessageWithArrayAndNoDefaultCtor
            {
                SomeWords = new string[0]
            };

            var result = ExecuteSerializer.ForMessage<MessageWithArrayAndNoDefaultCtor>(message);

            Assert.AreEqual(result.SomeWords, new string[0]);
        }

        [Test]
        public void CanSerializeNullableArrayWithNullString()
        {
            var message = new MessageWithNullableArray
            {
                SagaId = Guid.Empty,
                SomeInts = new int?[] { null }
            };

            using (var stream = new MemoryStream())
            {
                SerializerFactory.Create<MessageWithNullableArray>().Serialize(message, stream);
                stream.Position = 0;
                var reader = new StreamReader(stream);
                var xml = reader.ReadToEnd();

                var expected = XDocument.Parse(@"<?xml version=""1.0"" ?>
<MessageWithNullableArray xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
<SagaId>00000000-0000-0000-0000-000000000000</SagaId>
<SomeInts>
<NullableOfInt32>null</NullableOfInt32>
</SomeInts>
</MessageWithNullableArray>
");
                var actual = XDocument.Parse(xml);

                Assert.AreEqual(expected.ToString(), actual.ToString());
            }
        }

        [Test]
        public void CanDeserializeNullableArrayWithValueSetToNullString()
        {
            var xml = @"<?xml version=""1.0"" ?>
<MessageWithNullableArray xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
<SagaId>00000000-0000-0000-0000-000000000000</SagaId>
<SomeInts>
<NullableOfInt32>null</NullableOfInt32>
</SomeInts>
</MessageWithNullableArray>
";
            var data = Encoding.UTF8.GetBytes(xml);

            using (var stream = new MemoryStream(data))
            {
                var msgArray = SerializerFactory.Create<MessageWithNullableArray>().Deserialize(stream, new[] { typeof(MessageWithNullableArray) });
                var result = (MessageWithNullableArray)msgArray[0];

                Assert.AreEqual(null, result.SomeInts[0]);
            }
        }

        [Test]
        public void CanDeserializeNullableArrayWithFirstEntryXsiNilAttributeSetToTrue()
        {
            var xml = @"<?xml version=""1.0"" ?>
<MessageWithNullableArray xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
<SagaId>00000000-0000-0000-0000-000000000000</SagaId>
<SomeInts>
<NullableOfInt32 xsi:nil=""true""></NullableOfInt32>
</SomeInts>
</MessageWithNullableArray>
";
            var data = Encoding.UTF8.GetBytes(xml);

            using (var stream = new MemoryStream(data))
            {
                var msgArray = SerializerFactory.Create<MessageWithNullableArray>().Deserialize(stream, new[] { typeof(MessageWithNullableArray) });
                var result = (MessageWithNullableArray)msgArray[0];

                Assert.AreEqual(null, result.SomeInts[0]);
            }
        }

        [Test]
        public void CanDeserializeNullableArrayWithXsiNilAttributeSetToTrue()
        {
            var xml = @"<?xml version=""1.0"" ?>
<MessageWithNullableArray xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
<SagaId>00000000-0000-0000-0000-000000000000</SagaId>
<SomeInts xsi:nil=""true"">
</SomeInts>
</MessageWithNullableArray>
";
            var data = Encoding.UTF8.GetBytes(xml);

            using (var stream = new MemoryStream(data))
            {
                var msgArray = SerializerFactory.Create<MessageWithNullableArray>().Deserialize(stream, new[] { typeof(MessageWithNullableArray) });
                var result = (MessageWithNullableArray)msgArray[0];

                Assert.IsFalse(result.SomeInts.Any());
            }
        }

        [Test]
        public void CanDeserializeNullableArrayWithNoElementsToEmptyList()
        {
            var xml = @"<?xml version=""1.0"" ?>
<MessageWithNullableArray xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
<SagaId>00000000-0000-0000-0000-000000000000</SagaId>
<SomeInts>
</SomeInts>
</MessageWithNullableArray>
";
            var data = Encoding.UTF8.GetBytes(xml);

            using (var stream = new MemoryStream(data))
            {
                var msgArray = SerializerFactory.Create<MessageWithNullableArray>().Deserialize(stream, new[] { typeof(MessageWithNullableArray) });
                var result = (MessageWithNullableArray)msgArray[0];

                Assert.NotNull(result.SomeInts);
                Assert.AreEqual(0, result.SomeInts.Length);
            }
        }

        [Test]
        public void CanDeserializeNullableArrayWithValueSetToEmptyString()
        {
            var xml = @"<?xml version=""1.0"" ?>
<MessageWithNullableArray xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
<SagaId>00000000-0000-0000-0000-000000000000</SagaId>
<SomeInts>
<NullableOfInt32>
</NullableOfInt32>
</SomeInts>
</MessageWithNullableArray>
";
            var data = Encoding.UTF8.GetBytes(xml);

            using (var stream = new MemoryStream(data))
            {
                var msgArray = SerializerFactory.Create<MessageWithNullableArray>().Deserialize(stream, new[] { typeof(MessageWithNullableArray) });
                var result = (MessageWithNullableArray)msgArray[0];

                Assert.AreEqual(null, result.SomeInts[0]);
            }
        }

        [Test]
        public void CanSerializeMessageWithNullableArray()
        {
            // Issue https://github.com/Particular/NServiceBus/issues/2706
            var message = new MessageWithNullableArray
            {
                SomeInts = new int?[] { null, 1, null, 3, null }
            };

            var result = ExecuteSerializer.ForMessage<MessageWithNullableArray>(message);

            Assert.IsNull(result.SomeInts[0]);
            Assert.AreEqual(1, result.SomeInts[1]);
            Assert.IsNull(result.SomeInts[2]);
            Assert.AreEqual(3, result.SomeInts[3]);
            Assert.IsNull(result.SomeInts[4]);
        }
    }
}