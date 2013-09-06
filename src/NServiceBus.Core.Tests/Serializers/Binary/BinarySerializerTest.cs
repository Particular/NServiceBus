namespace NServiceBus.Core.Tests.Serializers.Binary
{
    using System;
    using System.IO;
    using System.Xml.Linq;

    using NServiceBus.Serializers.Binary;

    using NUnit.Framework;

    using System.Linq;

    [TestFixture]
    public class BinarySerializerTest
    {
        private BinaryMessageSerializer serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new BinaryMessageSerializer();
        }

        [Test]
        public void When_Using_Property_WithXContainerAssignable_should_serialize()
        {
            const string XmlElement = "<SomeClass xmlns=\"http://nservicebus.com\"><SomeProperty value=\"Bar\" /></SomeClass>";
            const string XmlDocument = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" + XmlElement;

            var messageWithXDocument = new MessageWithXDocument { Document = XDocument.Load(new StringReader(XmlDocument)) };
            var messageWithXElement = new MessageWithXElement { Document = XElement.Load(new StringReader(XmlElement)) };

            MessageWithXDocument resultXDocument;
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new object[] { messageWithXDocument }, stream);

                stream.Position = 0;

                resultXDocument = serializer.Deserialize(stream, new[] { typeof(MessageWithXDocument) }).OfType<MessageWithXDocument>().Single();
            }

            MessageWithXElement resultXElement;
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new object[] { messageWithXElement }, stream);

                stream.Position = 0;

                resultXElement = serializer.Deserialize(stream, new[] { typeof(MessageWithXElement) }).OfType<MessageWithXElement>().Single();
            }

            Assert.AreEqual(messageWithXDocument.Document.ToString(), resultXDocument.Document.ToString());
            Assert.AreEqual(messageWithXElement.Document.ToString(), resultXElement.Document.ToString());
        }

        [Serializable]
        public class MessageWithXDocument : IMessage
        {
            public XDocument Document { get; set; }
        }

        [Serializable]
        public class MessageWithXElement : IMessage
        {
            public XElement Document { get; set; }
        }
    }
}