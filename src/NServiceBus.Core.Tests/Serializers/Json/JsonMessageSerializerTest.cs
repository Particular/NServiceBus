namespace NServiceBus.Serializers.Json.Tests
{
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


        public class SimpleMessage1
        {
            public string PropertyOnMessage1 { get; set; }
        }

        public class SimpleMessage2
        {
            public string PropertyOnMessage2 { get; set; }
        }


        [Test]
        public void Deserialize_messages_wrapped_in_array_from_older_endpoint()
        {
            var jsonWithMultipleMessages = @"
[
  {
    $type: 'NServiceBus.Serializers.Json.Tests.JsonMessageSerializerTest+SimpleMessage1, NServiceBus.Core.Tests',
    PropertyOnMessage1: 'Message1'
  },
  {
    $type: 'NServiceBus.Serializers.Json.Tests.JsonMessageSerializerTest+SimpleMessage2, NServiceBus.Core.Tests',
    PropertyOnMessage2: 'Message2'
  }
]";
            using (var stream = new MemoryStream())
            {
                var streamWriter = new StreamWriter(stream);
                streamWriter.Write(jsonWithMultipleMessages);
                streamWriter.Flush();
                stream.Position = 0;
                var result = Serializer.Deserialize(stream, new[]
                {
                    typeof(SimpleMessage2),
                    typeof(SimpleMessage1)
                });

                Assert.AreEqual(2, result.Length);
                Assert.AreEqual("Message1", ((SimpleMessage1) result[0]).PropertyOnMessage1);
                Assert.AreEqual("Message2", ((SimpleMessage2) result[1]).PropertyOnMessage2);
            }
        }

        [Test]
        public void Deserialize_message_with_interface_without_wrapping()
        {
            using (var stream = new MemoryStream())
            {

                Serializer.Serialize(new SuperMessage {SomeProperty = "John"}, stream);

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
                Serializer.Serialize(new SimpleMessage(), stream);

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
                Serializer.Serialize(new SimpleMessage{SomeProperty = "test"}, stream);

                stream.Position = 0;
                var result = (SimpleMessage) Serializer.Deserialize(stream, new[]{typeof(SimpleMessage)})[0];

                Assert.AreEqual("test",result.SomeProperty);
            }

        }

        [Test]
        public void Serialize_message_without_typeInfo()
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(new SimpleMessage(), stream);

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
                Serializer.Serialize(mapper.CreateInstance<ISuperMessageWithoutConcreteImpl>(), stream);

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
                var msg = mapper.CreateInstance<ISuperMessageWithoutConcreteImpl>();
                msg.SomeProperty = "test";

                Serializer.Serialize(msg, stream);

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
                var msg = new SuperMessageWithConcreteImpl
                {
                    SomeProperty = "test"
                };

                Serializer.Serialize(msg, stream);

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
                Serializer.Serialize(messageWithXDocument, stream);

                stream.Position = 0;
                var json = new StreamReader(stream).ReadToEnd();
                stream.Position = 0;

                var result = Serializer.Deserialize(stream, new[] { typeof(MessageWithXDocument) }).Cast<MessageWithXDocument>().Single();

                Assert.AreEqual(messageWithXDocument.Document.ToString(), result.Document.ToString());
                Assert.AreEqual(XmlElement, json.Substring(13, json.Length - 15).Replace("\\", string.Empty));
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(messageWithXElement, stream);

                stream.Position = 0;
                var json = new StreamReader(stream).ReadToEnd();
                stream.Position = 0;

                var result = Serializer.Deserialize(stream, new[] { typeof(MessageWithXElement) }).Cast<MessageWithXElement>().Single();

                Assert.AreEqual(messageWithXElement.Document.ToString(), result.Document.ToString());
                Assert.AreEqual(XmlElement, json.Substring(13, json.Length - 15).Replace("\\", string.Empty));
            }
        }

        [Test]
        public void TestMany()
        {
            var xml = @"[{
    $type: ""NServiceBus.Serializers.Json.Tests.IA, NServiceBus.Core.Tests"",
    Data: ""rhNAGU4dr/Qjz6ocAsOs3wk3ZmxHMOg="",
    S: ""kalle"",
    I: 42,
    B: {
        BString: ""BOO"",
        C: {
            $type: ""NServiceBus.Serializers.Json.Tests.C, NServiceBus.Core.Tests"",
            Cstr: ""COO""
        }
    }
}, {
    $type: ""NServiceBus.Serializers.Json.Tests.IA, NServiceBus.Core.Tests"",
    Data: ""AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="",
    S: ""kalle"",
    I: 42,
    B: {
        BString: ""BOO"",
        C: {
            $type: ""NServiceBus.Serializers.Json.Tests.C, NServiceBus.Core.Tests"",
            Cstr: ""COO""
        }
    }
}]";
            using (var stream = new MemoryStream())
            {
                var streamWriter = new StreamWriter(stream);
                streamWriter.Write(xml);
                streamWriter.Flush();
                stream.Position = 0;

                var result = Serializer.Deserialize(stream);
                Assert.IsNotEmpty(result);
                Assert.That(result, Has.Length.EqualTo(2));

                Assert.That(result[0], Is.AssignableTo(typeof(IA)));
                var a = ((IA) result[0]);

                Assert.AreEqual(23, a.Data.Length);
                Assert.AreEqual(42, a.I);
                Assert.AreEqual("kalle", a.S);
                Assert.IsNotNull(a.B);
                Assert.AreEqual("BOO", a.B.BString);
                Assert.AreEqual("COO", ((C) a.B.C).Cstr);
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
