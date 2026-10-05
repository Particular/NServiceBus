namespace NServiceBus.Core.Tests.Serializers.XML;

using System;
using System.IO;
using System.Text;
using NServiceBus.Serializers.XML.Test;
using NServiceBus.Serializers.XML.Test.A;
using NServiceBus.Serializers.XML.Test.B;
using NUnit.Framework;

[TestFixture]
public class XmlMessageSerializerCharacterizationTests
{
    [Test]
    public void Should_deserialize_once_when_a_later_message_type_is_a_base_of_the_current_root()
    {
        var serializer = SerializerFactory.Create(typeof(DerivedXmlMessage), typeof(BaseXmlMessage));
        var body = Serialize(serializer, new DerivedXmlMessage { SomeProperty = "value", AdditionalProperty = "additional" });

        var result = serializer.Deserialize(body, [typeof(DerivedXmlMessage), typeof(BaseXmlMessage)]);

        Assert.That(result, Has.Length.EqualTo(1));
        Assert.That(result[0], Is.TypeOf<DerivedXmlMessage>());
    }

    [Test]
    public void Should_deserialize_each_message_type_when_neither_type_is_assignable_from_the_current_root()
    {
        var serializer = SerializerFactory.Create(typeof(BaseXmlMessage), typeof(DerivedXmlMessage));
        var body = Serialize(serializer, new DerivedXmlMessage { SomeProperty = "value", AdditionalProperty = "additional" });

        var result = serializer.Deserialize(body, [typeof(BaseXmlMessage), typeof(DerivedXmlMessage)]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Length.EqualTo(2));
            Assert.That(result[0], Is.TypeOf<BaseXmlMessage>());
            Assert.That(result[1], Is.TypeOf<DerivedXmlMessage>());
        }
    }

    [Test]
    public void Should_deserialize_all_messages_from_a_legacy_multi_message_payload()
    {
        var serializer = SerializerFactory.Create(typeof(Command1), typeof(Command2));
        var command1Id = Guid.NewGuid();
        var command2Id = Guid.NewGuid();

        var body = Encoding.UTF8.GetBytes(
            "<messages>" +
            SerializeBody(serializer, new Command1(command1Id)) +
            SerializeBody(serializer, new Command2(command2Id)) +
            "</messages>");

        var result = serializer.Deserialize(body, [typeof(Command1), typeof(Command2)]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Length.EqualTo(2));
            Assert.That(result[0], Is.TypeOf<Command1>());
            Assert.That(((Command1)result[0]).Id, Is.EqualTo(command1Id));
            Assert.That(result[1], Is.TypeOf<Command2>());
            Assert.That(((Command2)result[1]).Id, Is.EqualTo(command2Id));
        }
    }

    static byte[] Serialize(XmlMessageSerializer serializer, object message)
    {
        using var stream = new MemoryStream();
        serializer.Serialize(message, stream);
        return stream.ToArray();
    }

    static string SerializeBody(XmlMessageSerializer serializer, object message)
    {
        var xml = Encoding.UTF8.GetString(Serialize(serializer, message));
        return xml[(xml.IndexOf('>') + 1)..];
    }

    public class BaseXmlMessage : IMessage
    {
        public string SomeProperty { get; set; }
    }

    public class DerivedXmlMessage : BaseXmlMessage
    {
        public string AdditionalProperty { get; set; }
    }
}
