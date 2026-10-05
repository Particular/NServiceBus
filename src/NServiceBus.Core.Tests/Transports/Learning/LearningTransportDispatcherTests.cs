namespace NServiceBus.Core.Tests.Transports.Learning;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Routing;
using NUnit.Framework;
using Transport;

public class LearningTransportDispatcherTests
{
    [Test]
    public async Task Should_use_enclosed_message_hierarchy_for_multicast_subscribers()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, $"hierarchy-{Guid.NewGuid():N}");

        try
        {
            AddSubscriber(path, typeof(ConcreteEvent), "concrete-subscriber");
            AddSubscriber(path, typeof(BaseEvent), "base-subscriber");
            AddSubscriber(path, typeof(IEventContract), "interface-subscriber");
            AddSubscriber(path, typeof(IHeaderOnlyEventContract), "header-only-subscriber");
            AddSubscriber(path, typeof(IReflectedOnlyEventContract), "reflected-only-subscriber");
            AddSubscriber(path, typeof(IEvent), "marker-interface-subscriber");

            var hierarchy = string.Join(';', new[] { typeof(ConcreteEvent), typeof(BaseEvent), typeof(IEventContract), typeof(IHeaderOnlyEventContract), typeof(IEvent) }
                .Select(static type => type.AssemblyQualifiedName));
            var message = new OutgoingMessage("id", new Dictionary<string, string> { [Headers.EnclosedMessageTypes] = hierarchy }, ReadOnlyMemory<byte>.Empty);
            var operation = new TransportOperation(message, new MulticastAddressTag(typeof(ConcreteEvent)));
            var dispatcher = new LearningTransportDispatcher(path, 64);

            await dispatcher.Dispatch(new TransportOperations(operation), new TransportTransaction());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(HasDispatchedMessage(path, "concrete-subscriber"), Is.True);
                Assert.That(HasDispatchedMessage(path, "base-subscriber"), Is.True);
                Assert.That(HasDispatchedMessage(path, "interface-subscriber"), Is.True);
                Assert.That(HasDispatchedMessage(path, "header-only-subscriber"), Is.True);
                Assert.That(HasDispatchedMessage(path, "reflected-only-subscriber"), Is.False);
                Assert.That(HasDispatchedMessage(path, "marker-interface-subscriber"), Is.False);
            }
        }
        finally
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }

    [Test]
    public async Task Should_parse_generic_type_names_from_enclosed_message_hierarchy()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, $"generic-hierarchy-{Guid.NewGuid():N}");

        try
        {
            var eventType = typeof(GenericEvent<NestedEvent>);
            AddSubscriber(path, eventType, "generic-subscriber");

            var message = new OutgoingMessage("id", new Dictionary<string, string> { [Headers.EnclosedMessageTypes] = eventType.AssemblyQualifiedName! }, ReadOnlyMemory<byte>.Empty);
            var operation = new TransportOperation(message, new MulticastAddressTag(eventType));
            var dispatcher = new LearningTransportDispatcher(path, 64);

            await dispatcher.Dispatch(new TransportOperations(operation), new TransportTransaction());

            Assert.That(HasDispatchedMessage(path, "generic-subscriber"), Is.True);
        }
        finally
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }

    [Test]
    public async Task Should_infer_hierarchy_when_enclosed_message_types_header_is_missing()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, $"missing-hierarchy-{Guid.NewGuid():N}");

        try
        {
            AddSubscriber(path, typeof(ConcreteEvent), "concrete-subscriber");
            AddSubscriber(path, typeof(BaseEvent), "base-subscriber");
            AddSubscriber(path, typeof(IEventContract), "interface-subscriber");

            var message = new OutgoingMessage("id", [], ReadOnlyMemory<byte>.Empty);
            var operation = new TransportOperation(message, new MulticastAddressTag(typeof(ConcreteEvent)));
            var dispatcher = new LearningTransportDispatcher(path, 64);

            await dispatcher.Dispatch(new TransportOperations(operation), new TransportTransaction());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(HasDispatchedMessage(path, "concrete-subscriber"), Is.True);
                Assert.That(HasDispatchedMessage(path, "base-subscriber"), Is.True);
                Assert.That(HasDispatchedMessage(path, "interface-subscriber"), Is.True);
            }
        }
        finally
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }

    [Test]
    public async Task Should_throw_for_size_above_threshold()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "payload-too-big");
        var dispatcher = new LearningTransportDispatcher(path, 64);
        var headers = new Dictionary<string, string> { { Headers.EnclosedMessageTypes, "TestMessage" } };
        var messageAtThreshold = new OutgoingMessage("id", headers, new byte[MessageSizeLimit]);
        var messageAboveThreshold = new OutgoingMessage("id", headers, new byte[MessageSizeLimit + 1]);

        await dispatcher.Dispatch(new TransportOperations(new TransportOperation(messageAtThreshold, new UnicastAddressTag("my-destination"))), new TransportTransaction());
        var ex = Assert.ThrowsAsync<Exception>(async () => await dispatcher.Dispatch(new TransportOperations(new TransportOperation(messageAboveThreshold, new UnicastAddressTag("my-destination"))), new TransportTransaction()));

        Assert.That(ex.Message, Does.Contain("The total size of the 'TestMessage' message"));
    }

    static void AddSubscriber(string path, Type eventType, string subscriber)
    {
        var eventDirectory = Path.Combine(path, ".events", eventType.FullName!);
        Directory.CreateDirectory(eventDirectory);
        File.WriteAllText(Path.Combine(eventDirectory, $"{subscriber}.subscription"), subscriber);
    }

    static bool HasDispatchedMessage(string path, string subscriber) =>
        Directory.Exists(Path.Combine(path, subscriber)) && Directory.EnumerateFiles(Path.Combine(path, subscriber), "*.metadata.txt").Any();

    const int MessageSizeLimit = (64 * 1024) - headerSize;
    const int headerSize = 57;

    sealed class ConcreteEvent : BaseEvent, IEventContract, IReflectedOnlyEventContract;
    class BaseEvent : IEvent;
    interface IEventContract : IEvent;
    interface IHeaderOnlyEventContract : IEvent;
    interface IReflectedOnlyEventContract : IEvent;
}

sealed class GenericEvent<T> : IEvent;
sealed class NestedEvent;