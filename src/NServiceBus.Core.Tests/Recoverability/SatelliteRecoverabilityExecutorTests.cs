namespace NServiceBus.Core.Tests.Recoverability;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Transport;

[TestFixture]
public class SatelliteRecoverabilityExecutorTests
{
    [Test]
    public async Task Should_return_headers_to_the_pool_after_a_successful_dispatch()
    {
        var pool = new HeaderPool(maxPoolSize: 4);
        var dispatcher = new RecordingDispatcher();
        var executor = CreateExecutor(pool, dispatcher);

        await executor.Invoke(CreateErrorContext());

        var rented = pool.Rent();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(dispatcher.DispatchedHeaders, Is.Not.Null, "The failed message must be moved to the error queue.");
            Assert.That(rented, Is.SameAs(dispatcher.DispatchedHeaders), "The error message headers must be pooled once dispatch completed.");
            Assert.That(rented, Is.Empty);
        }
    }

    [Test]
    public async Task Should_not_return_headers_to_the_pool_when_dispatch_fails()
    {
        var pool = new HeaderPool(maxPoolSize: 4);
        var dispatcher = new RecordingDispatcher { Fail = true };
        var executor = CreateExecutor(pool, dispatcher);

        await Assert.ThrowsAsync<InvalidOperationException>(() => executor.Invoke(CreateErrorContext()));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(dispatcher.DispatchedHeaders, Does.ContainKey("existing-header"), "Headers of a failed dispatch must stay intact.");
            Assert.That(pool.Count, Is.Zero, "Headers of a failed dispatch must not be pooled.");
        }
    }

    static SatelliteRecoverabilityExecutor<object> CreateExecutor(HeaderPool pool, IMessageDispatcher dispatcher) =>
        new(
            new ServiceCollection().AddSingleton(dispatcher).BuildServiceProvider(),
            pool,
            new FaultMetadataExtractor([], _ => { }),
            (_, _) => RecoverabilityAction.MoveToError("error"),
            null);

    static ErrorContext CreateErrorContext() =>
        new(new Exception("test"), new Dictionary<string, string> { ["existing-header"] = "value" }, Guid.NewGuid().ToString(), Array.Empty<byte>(), new TransportTransaction(), 1, "receive address", new ContextBag());

    class RecordingDispatcher : IMessageDispatcher
    {
        public bool Fail { get; init; }

        public Dictionary<string, string> DispatchedHeaders { get; private set; }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
        {
            DispatchedHeaders = outgoingMessages.UnicastTransportOperations[0].Message.Headers;
            return Fail ? throw new InvalidOperationException("dispatch failed") : Task.CompletedTask;
        }
    }
}
