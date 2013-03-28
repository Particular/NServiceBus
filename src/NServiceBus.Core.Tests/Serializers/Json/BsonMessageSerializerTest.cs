namespace NServiceBus.Serializers.Json.Tests
{
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using NUnit.Framework;

    [TestFixture]
    public class BsonMessageSerializerTest : JsonMessageSerializerTestBase
    {
        [SetUp]
        public void Setup()
        {
            Serializer = new BsonMessageSerializer(MessageMapper);
        }

        [Test]
        public void When_Using_Property_WithXContainerAssignable_should_serialize()
        {
            const string XmlElement = "<SomeClass xmlns=\"http://nservicebus.com\"><SomeProperty value=\"Bar\" /></SomeClass>";
            const string XmlDocument = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" + XmlElement;

            var messageWithXDocument = new MessageWithXDocument { Document = XDocument.Load(new StringReader(XmlDocument)) };
            var messageWithXElement = new MessageWithXElement { Document = XElement.Load(new StringReader(XmlElement)) };

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(new object[] { messageWithXDocument }, stream);

                stream.Position = 0;

                var result = Serializer.Deserialize(stream, new[] { typeof(MessageWithXDocument) }).Cast<MessageWithXDocument>().Single();

                Assert.AreEqual(messageWithXDocument.Document.ToString(), result.Document.ToString());
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(new object[] { messageWithXElement }, stream);

                stream.Position = 0;

                var result = Serializer.Deserialize(stream, new[] { typeof(MessageWithXElement) }).Cast<MessageWithXElement>().Single();

                Assert.AreEqual(messageWithXElement.Document.ToString(), result.Document.ToString());
            }
        }
    }
}