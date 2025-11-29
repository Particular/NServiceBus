namespace NServiceBus.Core.Tests.Sagas;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

[TestFixture]
public class SagaNotFoundInvocationTests
{
    [Test]
    public async Task Should_dispose_finders()
    {
        await InvokeHandler<DisposableHandler>();
        Assert.That(DisposableHandler.DisposeCalled, Is.True);
    }

    [Test]
    public async Task Should_async_dispose_finders()
    {
        await InvokeHandler<AsyncDisposableHandler>();
        Assert.That(AsyncDisposableHandler.DisposeCalled, Is.True);
    }

    static async Task InvokeHandler<THandler>(CancellationToken cancellationToken = default) where THandler : NServiceBus.IHandleSagaNotFound
    {
        var services = new ServiceCollection();

        var invocation = new SagaNotFoundHandlerInvocation<THandler>();

        await using var serviceProvider = services.BuildServiceProvider();

        await invocation.Invoke(serviceProvider, null, null);
    }

    class DisposableHandler : IHandleSagaNotFound, IDisposable
    {
        public void Dispose() => DisposeCalled = true;

        public static bool DisposeCalled;
        public Task Handle(object message, IMessageProcessingContext context) => Task.CompletedTask;
    }

    class AsyncDisposableHandler : IHandleSagaNotFound, IAsyncDisposable
    {
        public static bool DisposeCalled;

        public ValueTask DisposeAsync()
        {
            DisposeCalled = true;
            return ValueTask.CompletedTask;
        }

        public Task Handle(object message, IMessageProcessingContext context) => Task.CompletedTask;
    }
}