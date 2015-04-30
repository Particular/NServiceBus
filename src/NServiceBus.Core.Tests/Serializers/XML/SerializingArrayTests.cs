namespace NServiceBus.Serializers.XML.Test
{
    using System;
    using System.IO;
    using System.Text;
    using NUnit.Framework;

    [Serializable]
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

    [Serializable]
    public class MessageWithArrayAndNoDefaultCtor
    {
        public Guid SagaId { get; set; }
        public string[] SomeWords { get; set; }
    }

    [Serializable]
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
                SerializerFactory.Create<MessageWithNullableArray>().Serialize(new[] { message }, stream);
                stream.Position = 0;
                var reader = new StreamReader(stream);
                var xml = reader.ReadToEnd().Replace("\r\n", "\n").Replace("\n", "\r\n");

                Assert.AreEqual(@"<?xml version=""1.0"" ?>
<Messages xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
<MessageWithNullableArray>
<SagaId>00000000-0000-0000-0000-000000000000</SagaId>
<SomeInts>
<NullableOfInt32>
null</NullableOfInt32>
</SomeInts>
</MessageWithNullableArray>
</Messages>
", xml);
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