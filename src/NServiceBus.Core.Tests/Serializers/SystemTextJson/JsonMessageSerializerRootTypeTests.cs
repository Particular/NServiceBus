namespace NServiceBus.Core.Tests.SystemTextJson;

using System;
using System.IO;
using System.Text.Json;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Serializers.SystemJson;
using NUnit.Framework;

[TestFixture]
public class JsonMessageSerializerRootTypeTests
{
    [Test]
    public void Should_deserialize_once_when_a_later_message_type_is_a_base_of_the_current_root()
    {
        var serializer = CreateSerializer();
        var body = Serialize(serializer, new DerivedMessage { SomeProperty = "value", AdditionalProperty = "additional" });

        var result = serializer.Deserialize(body, [typeof(DerivedMessage), typeof(BaseMessage)]);

        Assert.That(result, Has.Length.EqualTo(1));
        Assert.That(result[0], Is.TypeOf<DerivedMessage>());
    }

    [Test]
    public void Should_deserialize_each_message_type_when_neither_type_is_assignable_from_the_current_root()
    {
        var serializer = CreateSerializer();
        var body = Serialize(serializer, new DerivedMessage { SomeProperty = "value", AdditionalProperty = "additional" });

        var result = serializer.Deserialize(body, [typeof(BaseMessage), typeof(DerivedMessage)]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Length.EqualTo(2));
            Assert.That(result[0], Is.TypeOf<BaseMessage>());
            Assert.That(result[1], Is.TypeOf<DerivedMessage>());
        }
    }

    static JsonMessageSerializer CreateSerializer() => new(new JsonSerializerOptions(), ContentTypes.Json, new TrimmingSafeMessageMapper());

    static byte[] Serialize(JsonMessageSerializer serializer, object message)
    {
        using var stream = new MemoryStream();
        serializer.Serialize(message, stream);
        return stream.ToArray();
    }

    public class BaseMessage : IMessage
    {
        public string SomeProperty { get; set; }
    }

    public class DerivedMessage : BaseMessage
    {
        public string AdditionalProperty { get; set; }
    }
}
