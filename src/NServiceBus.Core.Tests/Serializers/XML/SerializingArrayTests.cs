namespace NServiceBus.Serializers.XML.Test;

using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;


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


public class MessageWithArrayAndNoDefaultCtor
{
    public Guid SagaId { get; set; }
    public string[] SomeWords { get; set; }
}


public class MessageWithNullableArray
{
    public Guid SagaId { get; set; }
    public int?[] SomeInts { get; set; }
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

        var messages = serializer.Deserialize(data);

        Assert.That(messages, Is.Not.Null);
        Assert.That(messages, Has.Length.EqualTo(1));

        Assert.That(messages[0], Is.TypeOf<MessageWithArray>());
        var m = (MessageWithArray)messages[0];

        Assert.That(m.SomeInts, Is.Not.Null);
        Assert.That(m.SomeInts, Has.Length.EqualTo(1));
    }

    [Test]
    public void CanSerializeAndBack()
    {
        var message = new MessageWithArray(Guid.NewGuid(), [1234, 5323]);

        var result = ExecuteSerializer.ForMessage<MessageWithArray>(message);

        Assert.That(result.SomeInts, Is.Not.Null);
        Assert.That(result.SomeInts, Has.Length.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.SomeInts[0], Is.EqualTo(1234));
            Assert.That(result.SomeInts[1], Is.EqualTo(5323));
        }
    }

    [Test]
    public void CanSerializeMessageWithNullArray()
    {
        var message = new MessageWithArrayAndNoDefaultCtor
        {
            SomeWords = null
        };

        var result = ExecuteSerializer.ForMessage<MessageWithArrayAndNoDefaultCtor>(message);

        Assert.That(result.SomeWords, Is.Null);
    }

    [Test]
    public void CanSerializeMessageWithEmptyArray()
    {
        var message = new MessageWithArrayAndNoDefaultCtor
        {
            SomeWords = Array.Empty<string>()
        };

        var result = ExecuteSerializer.ForMessage<MessageWithArrayAndNoDefaultCtor>(message);

        Assert.That(Array.Empty<string>(), Is.EqualTo(result.SomeWords));
    }

    [Test]
    public void CanSerializeNullableArrayWithNullString()
    {
        var message = new MessageWithNullableArray
        {
            SagaId = Guid.Empty,
            SomeInts = new int?[] { null }
        };

        using (var stream = new MemoryStream())
        {
            SerializerFactory.Create<MessageWithNullableArray>().Serialize(message, stream);
            stream.Position = 0;
            var reader = new StreamReader(stream);
            var xml = reader.ReadToEnd();

            var expected = XDocument.Parse(@"<?xml version=""1.0"" ?>
<MessageWithNullableArray xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
<SagaId>00000000-0000-0000-0000-000000000000</SagaId>
<SomeInts>
<NullableOfInt32>null</NullableOfInt32>
</SomeInts>
</MessageWithNullableArray>
");
            var actual = XDocument.Parse(xml);

            Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
        }
    }

    [Test]
    public void CanDeserializeNullableArrayWithValueSetToNullString()
    {
        var xml = @"<?xml version=""1.0"" ?>
<MessageWithNullableArray xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
<SagaId>00000000-0000-0000-0000-000000000000</SagaId>
<SomeInts>
<NullableOfInt32>null</NullableOfInt32>
</SomeInts>
</MessageWithNullableArray>
";
        var data = Encoding.UTF8.GetBytes(xml);

        var msgArray = SerializerFactory.Create<MessageWithNullableArray>().Deserialize(data, new[] { typeof(MessageWithNullableArray) });
        var result = (MessageWithNullableArray)msgArray[0];

        Assert.That(result.SomeInts[0], Is.EqualTo(null));
    }

    [Test]
    public void CanDeserializeNullableArrayWithFirstEntryXsiNilAttributeSetToTrue()
    {
        var xml = @"<?xml version=""1.0"" ?>
<MessageWithNullableArray xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
<SagaId>00000000-0000-0000-0000-000000000000</SagaId>
<SomeInts>
<NullableOfInt32 xsi:nil=""true""></NullableOfInt32>
</SomeInts>
</MessageWithNullableArray>
";
        var data = Encoding.UTF8.GetBytes(xml);

        var msgArray = SerializerFactory.Create<MessageWithNullableArray>().Deserialize(data, new[] { typeof(MessageWithNullableArray) });
        var result = (MessageWithNullableArray)msgArray[0];

        Assert.That(result.SomeInts[0], Is.EqualTo(null));
    }

    [Test]
    public void CanDeserializeNullableArrayWithXsiNilAttributeSetToTrue()
    {
        var xml = @"<?xml version=""1.0"" ?>
<MessageWithNullableArray xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
<SagaId>00000000-0000-0000-0000-000000000000</SagaId>
<SomeInts xsi:nil=""true"">
</SomeInts>
</MessageWithNullableArray>
";
        var data = Encoding.UTF8.GetBytes(xml);

        var msgArray = SerializerFactory.Create<MessageWithNullableArray>().Deserialize(data, new[] { typeof(MessageWithNullableArray) });
        var result = (MessageWithNullableArray)msgArray[0];

        Assert.That(result.SomeInts.Length, Is.EqualTo(0));
    }

    [Test]
    public void CanDeserializeNullableArrayWithNoElementsToEmptyList()
    {
        var xml = @"<?xml version=""1.0"" ?>
<MessageWithNullableArray xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
<SagaId>00000000-0000-0000-0000-000000000000</SagaId>
<SomeInts>
</SomeInts>
</MessageWithNullableArray>
";
        var data = Encoding.UTF8.GetBytes(xml);

        var msgArray = SerializerFactory.Create<MessageWithNullableArray>().Deserialize(data, new[] { typeof(MessageWithNullableArray) });
        var result = (MessageWithNullableArray)msgArray[0];

        Assert.That(result.SomeInts, Is.Not.Null);
        Assert.That(result.SomeInts.Length, Is.EqualTo(0));
    }

    [Test]
    public void CanDeserializeNullableArrayWithValueSetToEmptyString()
    {
        var xml = @"<?xml version=""1.0"" ?>
<MessageWithNullableArray xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
<SagaId>00000000-0000-0000-0000-000000000000</SagaId>
<SomeInts>
<NullableOfInt32>
</NullableOfInt32>
</SomeInts>
</MessageWithNullableArray>
";
        var data = Encoding.UTF8.GetBytes(xml);

        var msgArray = SerializerFactory.Create<MessageWithNullableArray>().Deserialize(data, new[] { typeof(MessageWithNullableArray) });
        var result = (MessageWithNullableArray)msgArray[0];

        Assert.That(result.SomeInts[0], Is.EqualTo(null));
    }

    [Test]
    public void CanSerializeMessageWithNullableArray()
    {
        // Issue https://github.com/Particular/NServiceBus/issues/2706
        var message = new MessageWithNullableArray
        {
            SomeInts = new int?[] { null, 1, null, 3, null }
        };

        var result = ExecuteSerializer.ForMessage<MessageWithNullableArray>(message);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.SomeInts[0], Is.Null);
            Assert.That(result.SomeInts[1], Is.EqualTo(1));
            Assert.That(result.SomeInts[2], Is.Null);
            Assert.That(result.SomeInts[3], Is.EqualTo(3));
            Assert.That(result.SomeInts[4], Is.Null);
        }
    }
}