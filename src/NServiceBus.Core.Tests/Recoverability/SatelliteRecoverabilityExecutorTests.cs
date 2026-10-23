namespace NServiceBus.Core.Tests.Recoverability;

using System;
using System.Collections.Generic;
using System.Linq;
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
    public async Task Should_return_header_dictionaries_to_the_pool_after_dispatch()
    {
        var headerPool = new HeaderPool(maxPoolSize: 4);
        var dispatcher = new CapturingDispatcher();
        await using var serviceProvider = new ServiceCollection()
            .AddSingleton<IMessageDispatcher>(dispatcher)
            .BuildServiceProvider();

        await CreateExecutor(serviceProvider, headerPool).Invoke(CreateErrorContext());

        Assert.That(headerPool.Rent(), Is.SameAs(dispatcher.DispatchedHeaders.Single()), "Dispatched header dictionaries must be recycled.");
    }

    [Test]
    public async Task Should_not_return_header_dictionaries_to_the_pool_when_dispatch_fails()
    {
        var headerPool = new HeaderPool(maxPoolSize: 4);
        var dispatcher = new CapturingDispatcher { ThrowOnDispatch = true };
        await using var serviceProvider = new ServiceCollection()
            .AddSingleton<IMessageDispatcher>(dispatcher)
            .BuildServiceProvider();

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateExecutor(serviceProvider, headerPool).Invoke(CreateErrorContext()));

        var headers = dispatcher.DispatchedHeaders.Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(headers, Is.Not.Empty, "A dictionary whose dispatch failed must not be cleared.");
            Assert.That(headerPool.Rent(), Is.Not.SameAs(headers), "A dictionary whose dispatch failed must not be pooled.");
        }
    }

    static SatelliteRecoverabilityExecutor<object> CreateExecutor(IServiceProvider serviceProvider, HeaderPool headerPool) =>
        new(serviceProvider, headerPool, new FaultMetadataExtractor([], _ => { }), (_, _) => RecoverabilityAction.MoveToError("error"), null);

    static ErrorContext CreateErrorContext() => new(new Exception("test"), [], Guid.NewGuid().ToString(), Array.Empty<byte>(), new TransportTransaction(), 1, "receive address", new ContextBag());

    class CapturingDispatcher : IMessageDispatcher
    {
        public bool ThrowOnDispatch { get; init; }

        public List<Dictionary<string, string>> DispatchedHeaders { get; } = [];

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
        {
            DispatchedHeaders.AddRange(outgoingMessages.UnicastTransportOperations.Select(_ => _.Message.Headers));

            if (ThrowOnDispatch)
            {
                throw new InvalidOperationException("Dispatch failed");
            }

            return Task.CompletedTask;
        }
    }
}
