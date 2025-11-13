namespace NServiceBus.Core.Tests.Sagas;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Fakes;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Persistence;
using NServiceBus.Sagas;
using NUnit.Framework;

[TestFixture]
public class CustomFinderAdapterTests
{
    [Test]
    public void Throws_friendly_exception_when_ISagaFinder_FindBy_returns_null()
    {
        Assert.That(async () => await InvokeFinder<ReturnsNullFinder>(), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
    }

    [Test]
    public async Task Should_dispose_finders()
    {
        await InvokeFinder<DisposableFinder>();

        Assert.That(DisposableFinder.DisposeCalled, Is.True);
    }

    [Test]
    public async Task Should_async_dispose_finders()
    {
        await InvokeFinder<AsyncDisposableFinder>();

        Assert.That(AsyncDisposableFinder.DisposeCalled, Is.True);
    }

    static async Task InvokeFinder<TFinder>(CancellationToken cancellationToken = default) where TFinder : ISagaFinder<SagaData, StartSagaMessage>
    {
        var services = new ServiceCollection();

        var customerFinderAdapter = new CustomFinderAdapter<TFinder, SagaData, StartSagaMessage>();

        await using var serviceProvider = services.BuildServiceProvider();

        await customerFinderAdapter.Find(serviceProvider, new FakeSynchronizedStorageSession(), new ContextBag(), new StartSagaMessage(), new Dictionary<string, string>(), cancellationToken);
    }


    class ReturnsNullFinder : ISagaFinder<SagaData, StartSagaMessage>
    {
        public Task<SagaData> FindBy(StartSagaMessage message, ISynchronizedStorageSession storageSession, IReadOnlyContextBag context, CancellationToken cancellationToken = default) => null;
    }

    class DisposableFinder : ISagaFinder<SagaData, StartSagaMessage>, IDisposable
    {
        public Task<SagaData> FindBy(StartSagaMessage message, ISynchronizedStorageSession storageSession, IReadOnlyContextBag context, CancellationToken cancellationToken = default) => Task.FromResult(new SagaData());

        public void Dispose() => DisposeCalled = true;

        public static bool DisposeCalled;
    }

    class AsyncDisposableFinder : ISagaFinder<SagaData, StartSagaMessage>, IAsyncDisposable
    {
        public Task<SagaData> FindBy(StartSagaMessage message, ISynchronizedStorageSession storageSession, IReadOnlyContextBag context, CancellationToken cancellationToken = default) => Task.FromResult(new SagaData());

        public static bool DisposeCalled;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async ValueTask DisposeAsync() => DisposeCalled = true;
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }

    class SagaData : ContainSagaData;

    class StartSagaMessage;
}