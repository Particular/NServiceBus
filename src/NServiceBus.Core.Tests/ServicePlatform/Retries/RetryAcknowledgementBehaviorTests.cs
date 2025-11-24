namespace NServiceBus.Core.Tests.ServicePlatform.Retries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NUnit.Framework;
using Testing;

[TestFixture]
public class RetryAcknowledgementBehaviorTests
{
    [Test]
    public async Task Should_confirm_successful_retries_to_acknowledgement_queue()
    {
        const string acknowledgementQueue = "configuredAcknowledgementQueue";
        var routingPipeline = new RoutingPipeline();
        var behavior = new RetryAcknowledgementBehavior();

        var context = SetupTestableContext(routingPipeline);
        // Set necessary SC headers
        context.Message.Headers[RetryAcknowledgementBehavior.RetryUniqueMessageIdHeaderKey] = Guid.NewGuid().ToString("N");
        context.Message.Headers[RetryAcknowledgementBehavior.RetryConfirmationQueueHeaderKey] = acknowledgementQueue;

        await behavior.Invoke(context, _ => Task.CompletedTask);

        var outgoingMessage = routingPipeline.ForkInvocations.Single();
        Assert.Multiple(() =>
        {
            Assert.That(
                    outgoingMessage.Message.Headers["ServiceControl.Retry.UniqueMessageId"],
                    Is.EqualTo(context.Message.Headers["ServiceControl.Retry.UniqueMessageId"]));

            Assert.That(outgoingMessage.Message.Headers.ContainsKey("ServiceControl.Retry.Successful"), Is.True);

            Assert.That(outgoingMessage.Message.Body.Length, Is.EqualTo(0));

            Assert.That(outgoingMessage.Message.Headers[Headers.ControlMessageHeader], Is.EqualTo(bool.TrueString));
        });

        var addressTag = outgoingMessage.RoutingStrategies.Single().Apply([]) as UnicastAddressTag;
        Assert.Multiple(() =>
        {
            Assert.That(addressTag.Destination, Is.EqualTo(acknowledgementQueue));

            Assert.That(context.Extensions.TryGet(out MarkAsAcknowledgedBehavior.State _), Is.True);
        });
    }

    [Test]
    public void Should_not_confirm_when_processing_fails()
    {
        var routingPipeline = new RoutingPipeline();
        var behavior = new RetryAcknowledgementBehavior();

        var context = SetupTestableContext(routingPipeline);
        // Set necessary SC headers
        context.Message.Headers["ServiceControl.Retry.UniqueMessageId"] = Guid.NewGuid().ToString("N");
        context.Message.Headers[RetryAcknowledgementBehavior.RetryConfirmationQueueHeaderKey] = "SomeQueue";

        var exception = new Exception("some pipeline failure");
        var thrownException = Assert.ThrowsAsync<Exception>(async () => await behavior.Invoke(context, _ => Task.FromException(exception)));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.SameAs(thrownException));
            Assert.That(routingPipeline.ForkInvocations.Count, Is.EqualTo(0));
        });
    }

    [Test]
    // A missing SC version header indicates an older version of SC that cannot handle the confirmation message yet
    public async Task Should_not_confirm_when_message_does_not_contain_acknowledgementQueue_header()
    {
        var routingPipeline = new RoutingPipeline();
        var behavior = new RetryAcknowledgementBehavior();

        var context = SetupTestableContext(routingPipeline);
        context.Message.Headers["ServiceControl.Retry.UniqueMessageId"] = Guid.NewGuid().ToString("N");

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.Multiple(() =>
        {
            Assert.That(routingPipeline.ForkInvocations.Count, Is.EqualTo(0));
            Assert.That(context.Extensions.TryGet(out MarkAsAcknowledgedBehavior.State _), Is.False);
        });
    }

    [Test]
    public async Task Should_not_confirm_when_message_does_not_contain_retry_header()
    {
        var routingPipeline = new RoutingPipeline();
        var behavior = new RetryAcknowledgementBehavior();

        var context = SetupTestableContext(routingPipeline);
        context.Message.Headers[RetryAcknowledgementBehavior.RetryConfirmationQueueHeaderKey] = "SomeQueue";

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.Multiple(() =>
        {
            Assert.That(routingPipeline.ForkInvocations.Count, Is.EqualTo(0));
            Assert.That(context.Extensions.TryGet(out MarkAsAcknowledgedBehavior.State _), Is.False);
        });
    }

    static TestableTransportReceiveContext SetupTestableContext(RoutingPipeline routingPipeline)
    {
        var context = new TestableTransportReceiveContext();

        //setup fork pipeline
        var serviceCollection = new ServiceCollection();
        var pipelineModifications = new PipelineModifications();
        pipelineModifications.AddAddition(
            RegisterStep.Create("routingFork", typeof(RoutingPipeline), "for testing", _ => routingPipeline));
        var pipelineCache = new PipelineCache(serviceCollection.BuildServiceProvider(), pipelineModifications);
        context.Extensions.Set<IPipelineCache>(pipelineCache);

        return context;
    }

    class RoutingPipeline : Behavior<IRoutingContext>
    {
        public List<IRoutingContext> ForkInvocations { get; } = [];

        public override Task Invoke(IRoutingContext context, Func<Task> next)
        {
            ForkInvocations.Add(context);
            return Task.CompletedTask;
        }
    }
}