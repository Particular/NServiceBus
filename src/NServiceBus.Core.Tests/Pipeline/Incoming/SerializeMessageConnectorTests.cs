namespace NServiceBus.Core.Tests.Pipeline.Incoming;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Serialization;
using Testing;
using Unicast.Messages;

[TestFixture]
public class SerializeMessageConnectorTests
{
    [Test]
    public async Task Should_set_content_type_header()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);

        registry.RegisterMessageTypes(
        [
            typeof(MyMessage)
        ]);

        var context = new TestableOutgoingLogicalMessageContext
        {
            Message = new OutgoingLogicalMessage(typeof(MyMessage), new MyMessage())
        };

        var behavior = new SerializeMessageConnector(new FakeSerializer("myContentType"), registry);

        await behavior.Invoke(context, c => Task.CompletedTask);

        Assert.That(context.Headers[Headers.ContentType], Is.EqualTo("myContentType"));
    }

    class FakeSerializer(string contentType) : IMessageSerializer
    {
        public void Serialize(object message, Stream stream)
        {
        }

        public object[] Deserialize(ReadOnlyMemory<byte> body, IList<Type> messageTypes = null) => throw new NotImplementedException();

        public string ContentType { get; } = contentType;
    }

    class MyMessage : IMessage;
}