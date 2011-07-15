using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.Serializers.XML.Test
{
    [Serializable]
    public class MessageWithArray : ISagaMessage
    {
        public Guid SagaId { get; set; }
        public int[] SomeInts { get; set; }

        public MessageWithArray(Guid sagaId, int[] someInts)
        {
            SagaId = sagaId;
            SomeInts = someInts;
        }
    }

    [TestFixture]
    public class SerializingArrayTests
    {
        [Test]
        public void CanDeserializeXmlWithWhitespace()
        {
            var str =
              @"<?xml version=""1.0"" encoding=""utf-8""?>
<Messages xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"" xmlns:baseType=""NServiceBus.Saga.ISagaMessage"">
    <MessageWithArray>
        <SagaId>6bddc475-22a5-433b-a3ed-9edf00e8e353</SagaId>
        <SomeInts>
            <Int32>1405154</Int32>
        </SomeInts>
    </MessageWithArray>
</Messages>";

            var data = Encoding.UTF8.GetBytes(str);

            var serializer = new MessageSerializer
            {
                MessageMapper = new MessageMapper(),
                MessageTypes = new List<Type> { typeof(MessageWithArray) }
            };

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
            var serializer = new MessageSerializer
            {
                MessageMapper = new MessageMapper(),
                MessageTypes = new List<Type> { typeof(MessageWithArray) }
            };

            var message = new MessageWithArray(Guid.NewGuid(), new int[] { 1234, 5323 });

            var stream = new MemoryStream();
            serializer.Serialize(new IMessage[] { message }, stream);

            stream.Position = 0;

            var messages = serializer.Deserialize(stream);

            Assert.NotNull(messages);
            Assert.That(messages, Has.Length.EqualTo(1));

            Assert.That(messages[0], Is.TypeOf(typeof(MessageWithArray)));
            var m = (MessageWithArray)messages[0];

            Assert.IsNotNull(m.SomeInts);
            Assert.That(m.SomeInts, Has.Length.EqualTo(2));
            Assert.AreEqual(1234, m.SomeInts[0]);
            Assert.AreEqual(5323, m.SomeInts[1]);
        }
    }
}