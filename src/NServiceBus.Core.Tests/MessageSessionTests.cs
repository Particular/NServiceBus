using NServiceBus.Core.Tests.Pipeline;

namespace NServiceBus.Core.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class MessageSessionTests
{
    [Test]
    public async Task Should_not_share_root_context_across_operations()
    {
        var messageOperations = new TestableMessageOperations();
        messageOperations.SendPipeline.OnInvoke = context =>
        {
            Assert.False(context.Extensions.TryGet<int>("test", out _));
            context.Extensions.SetOnRoot("test", 42);
        };

        var session = new MessageSession(null, messageOperations, null, CancellationToken.None);

        await session.Send(new object());
        await session.Send(new object());
        await session.Send(new object());
    }

    [Test]
    public void Should_propagate_endpoint_cancellation_status_to_context()
    {
        var messageOperations = new TestableMessageOperations();
        messageOperations.SendPipeline.OnInvoke = context =>
        {
            context.CancellationToken.ThrowIfCancellationRequested();
        };

        using var endpointCancellationTokenSource = new CancellationTokenSource();
        using var requestCancellationTokenSource = new CancellationTokenSource();
        var session = new MessageSession(null, messageOperations, null, endpointCancellationTokenSource.Token);

        endpointCancellationTokenSource.Cancel();
        Assert.ThrowsAsync<OperationCanceledException>(async () => await session.Send(new object(), requestCancellationTokenSource.Token));
    }

    [Test]
    public void Should_propagate_request_cancellation_status_to_context()
    {
        var messageOperations = new TestableMessageOperations();
        messageOperations.SendPipeline.OnInvoke = context =>
        {
            context.CancellationToken.ThrowIfCancellationRequested();
        };

        using var endpointCancellationTokenSource = new CancellationTokenSource();
        using var requestCancellationTokenSource = new CancellationTokenSource();
        var session = new MessageSession(null, messageOperations, null, endpointCancellationTokenSource.Token);

        requestCancellationTokenSource.Cancel();
        Assert.ThrowsAsync<OperationCanceledException>(async () => await session.Send(new object(), requestCancellationTokenSource.Token));
    }
}