namespace NServiceBus.Serializers.Json.Tests
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using MessageInterfaces.MessageMapper.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class JsonMessageSerializerTest : JsonMessageSerializerTestBase
    {
        public JsonMessageSerializerTest()
            : base(typeof(SimpleMessage))
        {
        }

        [SetUp]
        public void Setup()
        {
            Serializer = new JsonMessageSerializer(MessageMapper);
        }

        [Test]
        public void Deserialize_message_with_interface_without_wrapping()
        {
            using (var stream = new MemoryStream())
            {
                Serializer.SkipArrayWrappingForSingleMessages = true;

                Serializer.Serialize(new object[] { new SuperMessage {SomeProperty = "John"} }, stream);

                stream.Position = 0;

                var result = (SuperMessage)Serializer.Deserialize(stream, new[] { typeof(SuperMessage), typeof(IMyEvent) })[0];

                Assert.AreEqual("John", result.SomeProperty);
            }
        }

        [Test]
        public void Serialize_message_without_wrapping()
        {
            using (var stream = new MemoryStream())
            {
                Serializer.SkipArrayWrappingForSingleMessages = true;

                Serializer.Serialize(new object[] { new SimpleMessage() }, stream);

                stream.Position = 0;
                var result = new StreamReader(stream).ReadToEnd();

                Assert.That(!result.StartsWith("["), result);
            }
        }

        [Test]
        public void Deserialize_message_without_wrapping()
        {
            using (var stream = new MemoryStream())
            {
                Serializer.SkipArrayWrappingForSingleMessages = true;

                Serializer.Serialize(new object[] { new SimpleMessage{SomeProperty = "test"} }, stream);

                stream.Position = 0;
                var result = (SimpleMessage) Serializer.Deserialize(stream, new[]{typeof(SimpleMessage)})[0];

                Assert.AreEqual("test",result.SomeProperty);
            }

        }

        [Test]
        public void Serialize_message_without_typeinfo()
        {
            using (var stream = new MemoryStream())
            {
                Serializer.SkipArrayWrappingForSingleMessages = true;

                Serializer.Serialize(new object[] { new SimpleMessage() }, stream);

                stream.Position = 0;
                var result = new StreamReader(stream).ReadToEnd();

                Assert.That(!result.Contains("$type"), result);
            }
        }

        [Test]
        public void Serialize_message_without_concrete_implementation()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new []{ typeof(ISuperMessageWithoutConcreteImpl)});
            
            using (var stream = new MemoryStream())
            {
                Serializer.SkipArrayWrappingForSingleMessages = true;

                Serializer.Serialize(new object[] { mapper.CreateInstance<ISuperMessageWithoutConcreteImpl>() }, stream);

                stream.Position = 0;
                var result = new StreamReader(stream).ReadToEnd();

                Assert.That(!result.Contains("$type"), result);
                Assert.That(result.Contains("SomeProperty"), result);
            }
        }

        [Test]
        public void Deserialize_message_without_concrete_implementation()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[] { typeof(ISuperMessageWithoutConcreteImpl) });

            using (var stream = new MemoryStream())
            {
                Serializer.SkipArrayWrappingForSingleMessages = true;

                var msg = mapper.CreateInstance<ISuperMessageWithoutConcreteImpl>();
                msg.SomeProperty = "test";

                Serializer.Serialize(new object[] { msg }, stream);

                stream.Position = 0;

                var result = (ISuperMessageWithoutConcreteImpl)Serializer.Deserialize(stream, new[] { typeof(ISuperMessageWithoutConcreteImpl) })[0];

                Assert.AreEqual("test", result.SomeProperty);
            }
        }

        [Test]
        public void Deserialize_message_with_concrete_implementation_and_interface()
        {
            var map = new[] {typeof(SuperMessageWithConcreteImpl), typeof(ISuperMessageWithConcreteImpl)};
            var mapper = new MessageMapper();
            mapper.Initialize(map);

            using (var stream = new MemoryStream())
            {
                var msg = new SuperMessageWithConcreteImpl();
                msg.SomeProperty = "test";

                Serializer.Serialize(new object[] { msg }, stream);

                stream.Position = 0;

                var result = (ISuperMessageWithConcreteImpl)Serializer.Deserialize(stream, map)[0];

                Assert.IsInstanceOf<SuperMessageWithConcreteImpl>(result);
                Assert.AreEqual("test", result.SomeProperty);
            }
        }
        

        [Test]
        public void When_Using_Property_WithXContainerAssignable_should_preserve_xml()
        {
            const string XmlElement = "<SomeClass xmlns=\"http://nservicebus.com\"><SomeProperty value=\"Bar\" /></SomeClass>";
            const string XmlDocument = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" + XmlElement;

            var messageWithXDocument = new MessageWithXDocument { Document = XDocument.Load(new StringReader(XmlDocument)) };
            var messageWithXElement = new MessageWithXElement { Document = XElement.Load(new StringReader(XmlElement)) };

            using (var stream = new MemoryStream())
            {
                Serializer.SkipArrayWrappingForSingleMessages = true;

                Serializer.Serialize(new object[] { messageWithXDocument }, stream);

                stream.Position = 0;
                var json = new StreamReader(stream).ReadToEnd();
                stream.Position = 0;

                var result = Serializer.Deserialize(stream, new[] { typeof(MessageWithXDocument) }).Cast<MessageWithXDocument>().Single();

                Assert.AreEqual(messageWithXDocument.Document.ToString(), result.Document.ToString());
                Assert.AreEqual(XmlElement, json.Substring(13, json.Length - 15).Replace("\\", string.Empty));
            }

            using (var stream = new MemoryStream())
            {
                Serializer.SkipArrayWrappingForSingleMessages = true;

                Serializer.Serialize(new object[] { messageWithXElement }, stream);

                stream.Position = 0;
                var json = new StreamReader(stream).ReadToEnd();
                stream.Position = 0;

                var result = Serializer.Deserialize(stream, new[] { typeof(MessageWithXElement) }).Cast<MessageWithXElement>().Single();

                Assert.AreEqual(messageWithXElement.Document.ToString(), result.Document.ToString());
                Assert.AreEqual(XmlElement, json.Substring(13, json.Length - 15).Replace("\\", string.Empty));
            }
        }
    }

    public class SimpleMessage
    {
        public string SomeProperty { get; set; }
    }

    public class SuperMessage : IMyEvent
    {
        public string SomeProperty { get; set; }
    }

    public interface IMyEvent
    {
    }

    public class MessageWithXDocument
    {
        public XDocument Document { get; set; }
    }

    public class MessageWithXElement
    {
        public XElement Document { get; set; }
    }

    public interface ISuperMessageWithoutConcreteImpl : IMyEvent
    {
        string SomeProperty { get; set; }
    }

    public interface ISuperMessageWithConcreteImpl : IMyEvent
    {
        string SomeProperty { get; set; }
    }

    public class SuperMessageWithConcreteImpl : ISuperMessageWithConcreteImpl
    {
        public string SomeProperty { get; set; }
    }
}
