namespace NServiceBus.Core.Tests;

using Pipeline;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class MessageSessionTests
{
    [Test]
    public async Task Should_not_share_root_context_across_operations()
    {
        var readValues = new List<int>();
        var messageOperations = new TestableMessageOperations
        {
            SendPipeline =
            {
                OnInvoke = context =>
                {
                    // read existing value
                    if (context.Extensions.TryGet<int>("test", out var i))
                    {
                        readValues.Add(i);
                    }

                    // set value on root
                    context.Extensions.SetOnRoot("test", 42);
                }
            }
        };

        var session = new MessageSession(loggingSlot: new object());
        session.Initialize(new ThrowingServiceProvider(), messageOperations, new ThrowingPipelineCache(), CancellationToken.None);

        await session.Send(new object());
        await session.Send(new object());
        await session.Send(new object());

        Assert.That(readValues, Is.Empty, "writes should not leak to other pipeline invocations");
    }

    [Test]
    public void Should_propagate_endpoint_cancellation_status_to_context()
    {
        var messageOperations = new TestableMessageOperations
        {
            SendPipeline =
            {
                OnInvoke = context =>
                {
                    context.CancellationToken.ThrowIfCancellationRequested();
                }
            }
        };

        var session = new MessageSession(loggingSlot: new object());
        session.Initialize(new ThrowingServiceProvider(), messageOperations, new ThrowingPipelineCache(), new CancellationToken(true));

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await session.Send(new object(), CancellationToken.None));
    }

    [Test]
    public void Should_propagate_request_cancellation_status_to_context()
    {
        var messageOperations = new TestableMessageOperations
        {
            SendPipeline =
            {
                OnInvoke = context =>
                {
                    context.CancellationToken.ThrowIfCancellationRequested();
                }
            }
        };

        var session = new MessageSession(loggingSlot: new object());
        session.Initialize(new ThrowingServiceProvider(), messageOperations, new ThrowingPipelineCache(), CancellationToken.None);

        Assert.ThrowsAsync<OperationCanceledException>(async () => await session.Send(new object(), new CancellationToken(true)));
    }

    [Test]
    public async Task Deferred_session_should_wait_until_initialized()
    {
        var deferredSession = new MessageSession(loggingSlot: new object());
        var messageOperations = new TestableMessageOperations();

        var sendTask = deferredSession.Send(new object(), new SendOptions());

        await Task.Delay(50);
        Assert.That(sendTask.IsCompleted, Is.False);

        deferredSession.Initialize(new ThrowingServiceProvider(), messageOperations, new ThrowingPipelineCache(), CancellationToken.None);
        await sendTask;

        Assert.That(messageOperations.SendPipeline.LastContext, Is.Not.Null);
    }
}