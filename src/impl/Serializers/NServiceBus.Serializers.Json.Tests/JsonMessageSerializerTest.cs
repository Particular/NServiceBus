namespace NServiceBus.Serializers.Json.Tests
{
    using System.IO;
    using MessageInterfaces.MessageMapper.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class JsonMessageSerializerTest : JsonMessageSerializerTestBase
    {
        protected override JsonMessageSerializerBase Serializer { get; set; }

        [SetUp]
        public void Setup()
        {
            var messageMapper = new MessageMapper();
            messageMapper.Initialize(new[] { typeof(IA), typeof(A),typeof(SimpleMessage) });

            Serializer = new JsonMessageSerializer(messageMapper);
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

                Assert.That(!result.StartsWith("["),result);
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
                var result = (SimpleMessage) Serializer.Deserialize(stream, new[]{typeof(SimpleMessage).AssemblyQualifiedName})[0];

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
    }

    public class SimpleMessage
    {
        public string SomeProperty { get; set; }
    }
}
