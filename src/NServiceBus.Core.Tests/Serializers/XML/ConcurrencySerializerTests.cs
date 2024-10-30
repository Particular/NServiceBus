namespace NServiceBus.Serializers.XML.Test;

using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class ConcurrencySerializerTests
{
    [Test]
    public void Should_deserialize_in_parallel()
    {
        var expected = new RequestDataMessage
        {
            DataId = Guid.Empty,
            String = "<node>it's my \"node\" & i like it<node>",
        };

        var serializer = SerializerFactory.Create<RequestDataMessage>();

        Parallel.For(1, 1000, i =>
        {
            using var stream = new MemoryStream();

            serializer.Serialize(expected, stream);
            stream.Position = 0;

            var msgArray = serializer.Deserialize(stream.ToArray());
            var result = (RequestDataMessage)msgArray[0];

            Assert.That(result.DataId, Is.EqualTo(expected.DataId));
            Assert.That(result.String, Is.EqualTo(expected.String));
        });
    }
}

public class RequestDataMessage : IMessage
{
    public Guid DataId { get; set; }

    public string String { get; set; }
}