namespace NServiceBus.Core.Tests.Serializers.XML;

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
        using (var stream = new MemoryStream())
        {
            serializer.Serialize(message, stream);

            stream.Position = 0;

            messageDeserialized = serializer.Deserialize(stream.ToArray(), new[] { message.GetType() });
        }

        Assert.Multiple(() =>
        {
            Assert.That(((TestMessageWithChar)messageDeserialized[0]).InvalidCharacter, Is.EqualTo(message.InvalidCharacter));
            Assert.That(((TestMessageWithChar)messageDeserialized[0]).ValidCharacter, Is.EqualTo(message.ValidCharacter));
        });
    }

    public class TestMessageWithChar : IMessage
    {
        public char InvalidCharacter { get; set; }
        public char ValidCharacter { get; set; }
    }
}
