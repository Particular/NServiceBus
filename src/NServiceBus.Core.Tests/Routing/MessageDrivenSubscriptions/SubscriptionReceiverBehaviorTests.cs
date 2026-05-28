namespace NServiceBus.Core.Tests.Routing.MessageDrivenSubscriptions;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Testing;
using Transport;
using Unicast.Subscriptions;
using Unicast.Subscriptions.MessageDrivenSubscriptions;

[TestFixture]
public class SubscriptionReceiverBehaviorTests
{
    [Test]
    public async Task Should_call_next_for_regular_message()
    {
        var storage = new RecordingSubscriptionStorage();
        var context = CreateContext();
        context.Message.Headers[Headers.MessageIntent] = MessageIntent.Send.ToString();
        var nextCalled = false;

        await new SubscriptionReceiverBehavior(storage, _ => true).Invoke(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(nextCalled, Is.True);
            Assert.That(storage.SubscribeCalls, Is.EqualTo(0));
            Assert.That(storage.UnsubscribeCalls, Is.EqualTo(0));
        }
    }

    [Test]
    public void Should_throw_when_subscribe_intent_without_subscription_message_type()
    {
        var context = CreateContext();
        context.Message.Headers[Headers.MessageIntent] = MessageIntent.Subscribe.ToString();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SubscriptionReceiverBehavior(new RecordingSubscriptionStorage(), _ => true)
                .Invoke(context, _ => Task.CompletedTask));

        Assert.That(exception.Message, Is.EqualTo("Message intent is Subscribe, but the subscription message type header is missing."));
    }

    [Test]
    public void Should_throw_when_subscription_message_type_exists_with_non_subscription_intent()
    {
        var context = CreateContext();
        context.Message.Headers[Headers.MessageIntent] = MessageIntent.Send.ToString();
        context.Message.Headers[Headers.SubscriptionMessageType] = typeof(MyEvent).AssemblyQualifiedName;

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SubscriptionReceiverBehavior(new RecordingSubscriptionStorage(), _ => true)
                .Invoke(context, _ => Task.CompletedTask));

        Assert.That(exception.Message, Is.EqualTo("Subscription messages need to have intent set to Subscribe/Unsubscribe."));
    }

    [Test]
    public void Should_throw_when_no_subscriber_transport_address_and_no_reply_to_address()
    {
        var context = CreateContext();
        context.Message.Headers[Headers.MessageIntent] = MessageIntent.Subscribe.ToString();
        context.Message.Headers[Headers.SubscriptionMessageType] = typeof(MyEvent).AssemblyQualifiedName;

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SubscriptionReceiverBehavior(new RecordingSubscriptionStorage(), _ => true)
                .Invoke(context, _ => Task.CompletedTask));

        Assert.That(exception.Message, Is.EqualTo("Subscription message arrived without a valid ReplyToAddress."));
    }

    [Test]
    public async Task Should_use_subscriber_transport_address_and_endpoint_when_present()
    {
        var storage = new RecordingSubscriptionStorage();
        var context = CreateContext();
        context.Message.Headers[Headers.MessageIntent] = MessageIntent.Subscribe.ToString();
        context.Message.Headers[Headers.SubscriptionMessageType] = typeof(MyEvent).AssemblyQualifiedName;
        context.Message.Headers[Headers.SubscriberTransportAddress] = "subscriber-address";
        context.Message.Headers[Headers.SubscriberEndpoint] = "subscriber-endpoint";

        await new SubscriptionReceiverBehavior(storage, _ => true).Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(storage.SubscribeCalls, Is.EqualTo(1));
            Assert.That(storage.LastSubscriber.TransportAddress, Is.EqualTo("subscriber-address"));
            Assert.That(storage.LastSubscriber.Endpoint, Is.EqualTo("subscriber-endpoint"));
        }
    }

    [Test]
    public async Task Should_fall_back_to_reply_to_address_when_subscriber_transport_address_missing()
    {
        var storage = new RecordingSubscriptionStorage();
        var context = CreateContext();
        context.Message.Headers[Headers.MessageIntent] = MessageIntent.Subscribe.ToString();
        context.Message.Headers[Headers.SubscriptionMessageType] = typeof(MyEvent).AssemblyQualifiedName;
        context.Message.Headers[Headers.ReplyToAddress] = "reply-to-address";

        await new SubscriptionReceiverBehavior(storage, _ => true).Invoke(context, _ => Task.CompletedTask);

        Assert.That(storage.LastSubscriber.TransportAddress, Is.EqualTo("reply-to-address"));
    }

    [Test]
    public async Task Should_call_subscribe_for_subscribe_intent_and_not_call_next()
    {
        var storage = new RecordingSubscriptionStorage();
        var context = CreateContext();
        context.Message.Headers[Headers.MessageIntent] = MessageIntent.Subscribe.ToString();
        context.Message.Headers[Headers.SubscriptionMessageType] = typeof(MyEvent).AssemblyQualifiedName;
        context.Message.Headers[Headers.ReplyToAddress] = "reply-to";
        var nextCalled = false;

        await new SubscriptionReceiverBehavior(storage, _ => true).Invoke(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(storage.SubscribeCalls, Is.EqualTo(1));
            Assert.That(storage.UnsubscribeCalls, Is.EqualTo(0));
            Assert.That(nextCalled, Is.False);
        }
    }

    [Test]
    public async Task Should_call_unsubscribe_for_unsubscribe_intent_and_not_call_next()
    {
        var storage = new RecordingSubscriptionStorage();
        var context = CreateContext();
        context.Message.Headers[Headers.MessageIntent] = MessageIntent.Unsubscribe.ToString();
        context.Message.Headers[Headers.SubscriptionMessageType] = typeof(MyEvent).AssemblyQualifiedName;
        context.Message.Headers[Headers.ReplyToAddress] = "reply-to";
        var nextCalled = false;

        await new SubscriptionReceiverBehavior(storage, _ => true).Invoke(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(storage.SubscribeCalls, Is.EqualTo(0));
            Assert.That(storage.UnsubscribeCalls, Is.EqualTo(1));
            Assert.That(nextCalled, Is.False);
        }
    }

    [Test]
    public async Task Should_not_call_storage_and_not_call_next_when_authorizer_returns_false()
    {
        var storage = new RecordingSubscriptionStorage();
        var context = CreateContext();
        context.Message.Headers[Headers.MessageIntent] = MessageIntent.Subscribe.ToString();
        context.Message.Headers[Headers.SubscriptionMessageType] = typeof(MyEvent).AssemblyQualifiedName;
        context.Message.Headers[Headers.ReplyToAddress] = "reply-to";
        var nextCalled = false;

        await new SubscriptionReceiverBehavior(storage, _ => false).Invoke(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(storage.SubscribeCalls, Is.EqualTo(0));
            Assert.That(storage.UnsubscribeCalls, Is.EqualTo(0));
            Assert.That(nextCalled, Is.False);
        }
    }

    [Test]
    public async Task Should_not_call_next_when_subscription_storage_is_null()
    {
        var context = CreateContext();
        context.Message.Headers[Headers.MessageIntent] = MessageIntent.Subscribe.ToString();
        context.Message.Headers[Headers.SubscriptionMessageType] = typeof(MyEvent).AssemblyQualifiedName;
        context.Message.Headers[Headers.ReplyToAddress] = "reply-to";
        var nextCalled = false;

        await new SubscriptionReceiverBehavior(null, _ => true).Invoke(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.That(nextCalled, Is.False);
    }

    static TestableIncomingPhysicalMessageContext CreateContext() => new()
    {
        Message = new IncomingMessage(Guid.NewGuid().ToString(), [], Array.Empty<byte>())
    };

    class RecordingSubscriptionStorage : ISubscriptionStorage
    {
        public int SubscribeCalls { get; private set; }
        public int UnsubscribeCalls { get; private set; }
        public Subscriber LastSubscriber { get; private set; }

        public Task Subscribe(Subscriber subscriber, MessageType messageType, Extensibility.ContextBag context, CancellationToken cancellationToken = default)
        {
            SubscribeCalls++;
            LastSubscriber = subscriber;
            return Task.CompletedTask;
        }

        public Task Unsubscribe(Subscriber subscriber, MessageType messageType, Extensibility.ContextBag context, CancellationToken cancellationToken = default)
        {
            UnsubscribeCalls++;
            LastSubscriber = subscriber;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, Extensibility.ContextBag context, CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Subscriber>>([]);
    }

    class MyEvent : IEvent;
}
