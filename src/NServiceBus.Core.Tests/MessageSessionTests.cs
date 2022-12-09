namespace NServiceBus.Core.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
using MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Pipeline;
using NUnit.Framework;

[TestFixture]
public class MessageSessionTests
{
    [Test]
    public async Task Should_not_share_root_context_across_operations()
    {
        var sendPipeline = new FakePipeline<IOutgoingSendContext>(context =>
        {
            Assert.False(context.Extensions.TryGet<int>("test", out _));
            context.Extensions.SetOnRoot("test", 42);
        });

        var messageOperations = CreateMessageOperations(sendPipeline);
        var session = new MessageSession(null, messageOperations, null, CancellationToken.None);

        await session.Send(new object());
        await session.Send(new object());
        await session.Send(new object());
    }

    [Test]
    public void Should_propagate_endpoint_cancellation_status_to_context()
    {
        var sendPipeline = new FakePipeline<IOutgoingSendContext>(context =>
        {
            context.CancellationToken.ThrowIfCancellationRequested();
        });

        var messageOperations = CreateMessageOperations(sendPipeline);
        using var endpointCancellationTokenSource = new CancellationTokenSource();
        using var requestCancellationTokenSource = new CancellationTokenSource();
        var session = new MessageSession(null, messageOperations, null, endpointCancellationTokenSource.Token);

        endpointCancellationTokenSource.Cancel();
        Assert.ThrowsAsync<OperationCanceledException>(async () => await session.Send(new object(), requestCancellationTokenSource.Token));
    }

    [Test]
    public void Should_propagate_request_cancellation_status_to_context()
    {
        var sendPipeline = new FakePipeline<IOutgoingSendContext>(context =>
        {
            context.CancellationToken.ThrowIfCancellationRequested();
        });

        var messageOperations = CreateMessageOperations(sendPipeline);
        using var endpointCancellationTokenSource = new CancellationTokenSource();
        using var requestCancellationTokenSource = new CancellationTokenSource();
        var session = new MessageSession(null, messageOperations, null, endpointCancellationTokenSource.Token);

        requestCancellationTokenSource.Cancel();
        Assert.ThrowsAsync<OperationCanceledException>(async () => await session.Send(new object(), requestCancellationTokenSource.Token));
    }

    private static MessageOperations CreateMessageOperations(FakePipeline<IOutgoingSendContext> sendPipeline)
    {
        var messageOperations = new MessageOperations(new MessageMapper(), new FakePipeline<IOutgoingPublishContext>(),
            sendPipeline, new FakePipeline<IOutgoingReplyContext>(),
            new FakePipeline<ISubscribeContext>(), new FakePipeline<IUnsubscribeContext>(), new ActivityFactory());
        return messageOperations;
    }


    class FakePipeline<TContext> : IPipeline<TContext> where TContext : IBehaviorContext
    {
        readonly Action<TContext> customAction;

        public FakePipeline(Action<TContext> customAction = null)
        {
            this.customAction = customAction;
        }

        public Task Invoke(TContext context)
        {
            customAction?.Invoke(context);
            return Task.CompletedTask;
        }
    }
}