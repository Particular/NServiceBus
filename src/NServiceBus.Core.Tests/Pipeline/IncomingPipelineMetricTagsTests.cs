namespace NServiceBus.Core.Tests.Pipeline.Incoming;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Serialization;
using Testing;
using Transport;
using Unicast.Messages;

[TestFixture]
public class IncomingPipelineMetricTagsTests
{
    [Test]
    public void Should_not_fail_when_handling_more_than_one_logical_message()
    {
        var registry = new MessageMetadataRegistry(new Conventions().IsMessageType, true);

        registry.RegisterMessageTypes(
        [
            typeof(MyMessage)
        ]);

        var context = new TestableIncomingPhysicalMessageContext
        {
            Message = new IncomingMessage("messageId", new Dictionary<string, string>
            {
                { Headers.EnclosedMessageTypes, typeof(MyMessage).AssemblyQualifiedName }
            }, new ReadOnlyMemory<byte>(new byte[] { 1 }))
        };

        var messageMapper = new MessageMapper();
        var behavior = new DeserializeMessageConnector(new MessageDeserializerResolver(new FakeSerializer(), []), new LogicalMessageFactory(registry, messageMapper), registry, messageMapper, false);

        Assert.DoesNotThrowAsync(async () => await behavior.Invoke(context, c =>
        {
            c.Extensions.Get<IncomingPipelineMetricTags>().Add("Same", "Same");
            return Task.CompletedTask;
        }));
    }

    class MyMessage : IMessage { }

    class FakeSerializer : IMessageSerializer
    {
        public string ContentType { get; }
        public void Serialize(object message, Stream stream) => throw new NotImplementedException();

        public object[] Deserialize(ReadOnlyMemory<byte> body, IList<Type> messageTypes = null) => [new MyMessage(), new MyMessage()];
    }
}