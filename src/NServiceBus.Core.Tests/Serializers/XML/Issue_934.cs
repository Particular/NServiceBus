namespace NServiceBus.Core.Tests.Serializers.XML
{
    using System.IO;
    using NServiceBus.Serializers.XML.Test;
    using NUnit.Framework;

    [TestFixture]
    public class Issue_934
    {
        [Test]
        public void Serialize_ShouldSucceed_WhenCharContainsXmlSpecialCharacters()
        {
            var serializer = SerializerFactory.Create<TestMessageWithChar>();
            var message = new TestMessageWithChar
                {
                    ValidCharacter = 'a',
                    InvalidCharacter = '<'
                };

            object[] messageDeserialized;
            using (Stream stream = new MemoryStream())
            {
                serializer.Serialize(new object[] { message }, stream);

                stream.Position = 0;

                messageDeserialized = serializer.Deserialize(stream, new[] { message.GetType() });
            }

            Assert.AreEqual(message.InvalidCharacter, ((TestMessageWithChar)messageDeserialized[0]).InvalidCharacter);
            Assert.AreEqual(message.ValidCharacter, ((TestMessageWithChar)messageDeserialized[0]).ValidCharacter);
        }

        public class TestMessageWithChar : IMessage
        {
            public char InvalidCharacter { get; set; }
            public char ValidCharacter { get; set; }
        }
    }
}
