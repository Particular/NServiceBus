namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using EndpointTemplates;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_processing_message_with_multiple_handlers : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_create_message_handler_spans()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<ReceivingEndpoint>(b =>
                b.When(session => session.SendLocal(new SomeMessage()))
            )
            .Run();

        var invokedHandlerActivities = NServiceBusActivityListener.CompletedActivities.GetInvokedHandlerActivities();
        var receivePipelineActivities = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invokedHandlerActivities, Has.Count.EqualTo(2), "a dedicated span for each handler should be created");
            Assert.That(receivePipelineActivities, Has.Count.EqualTo(1), "the receive pipeline should be invoked once");
        }

        var recordedHandlerTypes = new HashSet<string>();

        foreach (var invokedHandlerActivity in invokedHandlerActivities)
        {
            var handlerTypeTag = invokedHandlerActivity.GetTagItem("nservicebus.handler.handler_type") as string;
            Assert.That(handlerTypeTag, Is.Not.Null, "Handler type tag should be set");
            recordedHandlerTypes.Add(handlerTypeTag);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(invokedHandlerActivity.ParentId, Is.EqualTo(receivePipelineActivities[0].Id));
                Assert.That(invokedHandlerActivity.Status, Is.EqualTo(ActivityStatusCode.Ok));
            }

            Assert.That(invokedHandlerActivity.GetTagItem("custom_handler_tag"), Is.Not.Null, "Custom tag should be set");
        }

        Assert.That(recordedHandlerTypes, Does.Contain(typeof(ReceivingEndpoint.HandlerOne).FullName), "invocation of handler one should be traced");
        Assert.That(recordedHandlerTypes, Does.Contain(typeof(ReceivingEndpoint.HandlerTwo).FullName), "invocation of handler two should be traced");
    }

    class Context : ScenarioContext
    {
        public bool FirstHandlerRun { get; set; }
        public bool SecondHandlerRun { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(FirstHandlerRun && SecondHandlerRun);
    }

    class ReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReceivingEndpoint() => EndpointSetup<DefaultServer>(c => c.Pipeline.Register(typeof(AddTagToHandlerSpanBehavior), "Adds a custom tag to the handler span"));

        class AddTagToHandlerSpanBehavior : Behavior<IInvokeHandlerContext>
        {
            public override async Task Invoke(IInvokeHandlerContext context, Func<Task> next)
            {
                Activity.Current?.AddTag("custom_handler_tag", "some value");
                await next();
            }
        }

        public class HandlerOne(Context testContext) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.FirstHandlerRun = true;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }

        public class HandlerTwo(Context testContext) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.SecondHandlerRun = true;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class SomeMessage : IMessage
    {
    }
}