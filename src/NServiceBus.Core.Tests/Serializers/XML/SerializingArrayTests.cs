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

            Assert.IsNull(message.SomeWords);
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
    }
}