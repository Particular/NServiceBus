#nullable enable

namespace NServiceBus.Core.Tests.Pipeline.Outgoing;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NServiceBus.Routing;
using NServiceBus.Transport;
using NServiceBus.Utils;

[TestFixture]
public class ImmediateDispatchTerminatorTests
{
    [Test]
    public async Task Should_return_all_header_dictionaries_to_the_pool_after_dispatch()
    {
        var pool = NewPool();
        var first = RentHeaders(pool, "message-1");
        var second = RentHeaders(pool, "message-2");

        var dispatcher = new FakeDispatcher();
        var terminator = new ImmediateDispatchTerminator(dispatcher, pool);

        await terminator.Invoke(NewDispatchContext(Operation(first), Operation(second)), _ => Task.CompletedTask);

        Assert.That(dispatcher.UnicastTransportOperations, Has.Count.EqualTo(2), "Dispatch must receive all operations.");

        // ConcurrentStack is LIFO: the second dictionary was returned last, so it is rented out first.
        var rentedSecond = pool.Rent();
        var rentedFirst = pool.Rent();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(rentedSecond, Is.SameAs(second), "The second dictionary must be recycled.");
            Assert.That(rentedFirst, Is.SameAs(first), "The first dictionary must be recycled.");
            Assert.That(rentedSecond, Is.Empty, "Returned dictionaries must be cleared.");
            Assert.That(rentedFirst, Is.Empty, "Returned dictionaries must be cleared.");
        }
    }

    [Test]
    public void Should_not_return_header_dictionaries_to_the_pool_when_dispatch_fails()
    {
        var pool = NewPool();
        var headers = RentHeaders(pool, "message-1");

        var terminator = new ImmediateDispatchTerminator(new ThrowingDispatcher(), pool);

        Assert.ThrowsAsync<InvalidOperationException>(() => terminator.Invoke(NewDispatchContext(Operation(headers)), _ => Task.CompletedTask));

        var rentedAfterFailure = pool.Rent();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(headers["NServiceBus.MessageId"], Is.EqualTo("message-1"),
                "A dictionary whose dispatch failed must not be cleared.");
            Assert.That(rentedAfterFailure, Is.Not.SameAs(headers),
                "A dictionary whose dispatch failed must not be handed out again.");
            Assert.That(pool.Count, Is.Zero, "A dictionary whose dispatch failed must not be pooled.");
        }
    }

    [Test]
    public async Task Should_pool_only_dictionaries_owned_by_the_pool()
    {
        var pool = NewPool();
        var rented = RentHeaders(pool, "message-1");
        var foreign = new Dictionary<string, string> { ["NServiceBus.MessageId"] = "message-2" };

        var terminator = new ImmediateDispatchTerminator(new FakeDispatcher(), pool);

        await terminator.Invoke(NewDispatchContext(Operation(rented), Operation(foreign)), _ => Task.CompletedTask);

        var recycled = pool.Rent();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(recycled, Is.SameAs(rented), "Pool-owned dictionaries must be recycled.");
            Assert.That(pool.Count, Is.Zero, "Only the pool-owned dictionary may enter the pool.");
            Assert.That(foreign["NServiceBus.MessageId"], Is.EqualTo("message-2"),
                "Foreign dictionaries must not be cleared.");
        }
    }

    static DictionaryPool<string, string> NewPool() => new(maxPoolSize: 4);

    static Dictionary<string, string> RentHeaders(DictionaryPool<string, string> pool, string messageId)
    {
        var headers = pool.Rent();
        headers["NServiceBus.MessageId"] = messageId;
        return headers;
    }

    static DispatchContext NewDispatchContext(params TransportOperation[] operations) =>
        new(operations, new FakeRootContext());

    static TransportOperation Operation(Dictionary<string, string> headers) =>
        new(new OutgoingMessage(headers["NServiceBus.MessageId"], headers, Array.Empty<byte>()), new UnicastAddressTag("destination"));

    class FakeDispatcher : IMessageDispatcher
    {
        public List<UnicastTransportOperation> UnicastTransportOperations = [];

        public Task Dispatch(TransportOperations messages, TransportTransaction transportTransaction, CancellationToken cancellationToken = default)
        {
            UnicastTransportOperations = messages.UnicastTransportOperations;
            return Task.CompletedTask;
        }
    }

    class ThrowingDispatcher : IMessageDispatcher
    {
        public Task Dispatch(TransportOperations messages, TransportTransaction transportTransaction, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Dispatch failed");
    }
}
