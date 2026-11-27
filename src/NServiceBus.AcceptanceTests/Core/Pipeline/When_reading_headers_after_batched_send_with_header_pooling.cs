namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_reading_headers_after_batched_send_with_header_pooling : NServiceBusAcceptanceTest
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Should_read_headers_after_next_in_outgoing_and_receive_behaviors(bool enableHeaderPooling)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b
                .CustomConfig(c =>
                {
                    if (enableHeaderPooling)
                    {
                        c.EnableHeaderPooling();
                    }
                })
                .When(session =>
                {
                    var options = new SendOptions();
                    options.RouteToThisEndpoint();
                    return session.Send(new TriggerMessage(), options);
                }))
            .Done(c => c.FollowUpReceived && c.ReceiveBehaviorCompleted)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ExceptionInOutgoingBehavior, Is.Null, $"Reading headers after next() in the outgoing behavior threw: {context.ExceptionInOutgoingBehavior}");
            Assert.That(context.MessageIdInOutgoingBehaviorAfterNext, Is.EqualTo(context.MessageIdInOutgoingBehaviorBeforeNext));
            Assert.That(context.ExceptionInReceiveBehavior, Is.Null, $"Reading headers after next() in the receive behavior threw: {context.ExceptionInReceiveBehavior}");
            Assert.That(context.MessageIdInReceiveBehaviorAfterNext, Is.EqualTo(context.MessageIdInOutgoingBehaviorBeforeNext));
        }
    }

    public class Context : ScenarioContext
    {
        public Dictionary<string, string> BatchedHeaders { get; set; }
        public string MessageIdInOutgoingBehaviorBeforeNext { get; set; }
        public string MessageIdInOutgoingBehaviorAfterNext { get; set; }
        public Exception ExceptionInOutgoingBehavior { get; set; }
        public string MessageIdInReceiveBehaviorAfterNext { get; set; }
        public Exception ExceptionInReceiveBehavior { get; set; }
        public bool ReceiveBehaviorCompleted { get; set; }
        public bool FollowUpReceived { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>(c =>
        {
            c.Pipeline.Register(b => new OutgoingHeadersBehavior(b.GetService<Context>()), "Reads the outgoing headers after the rest of the outgoing pipeline has run");
            c.Pipeline.Register(b => new ReceiveHeadersBehavior(b.GetService<Context>()), "Reads the batched outgoing headers after the batch has been dispatched");
        });

        public class OutgoingHeadersBehavior(Context testContext) : Behavior<IOutgoingLogicalMessageContext>
        {
            public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
            {
                if (context.Message.MessageType != typeof(FollowUpMessage))
                {
                    await next();
                    return;
                }

                testContext.BatchedHeaders = context.Headers;
                testContext.MessageIdInOutgoingBehaviorBeforeNext = context.Headers[Headers.MessageId];

                await next();

                try
                {
                    testContext.MessageIdInOutgoingBehaviorAfterNext = context.Headers[Headers.MessageId];
                }
                catch (KeyNotFoundException ex)
                {
                    testContext.ExceptionInOutgoingBehavior = ex;
                }
            }
        }

        public class ReceiveHeadersBehavior(Context testContext) : Behavior<ITransportReceiveContext>
        {
            public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
            {
                await next();

                // Only the trigger invocation owns the batched headers; the follow-up can be received before its root returns them.
                if (!context.Message.Headers.TryGetValue(Headers.EnclosedMessageTypes, out var enclosedMessageTypes) ||
                    !enclosedMessageTypes.Contains(typeof(TriggerMessage).FullName!))
                {
                    return;
                }

                try
                {
                    testContext.MessageIdInReceiveBehaviorAfterNext = testContext.BatchedHeaders[Headers.MessageId];
                }
                catch (KeyNotFoundException ex)
                {
                    testContext.ExceptionInReceiveBehavior = ex;
                }

                testContext.ReceiveBehaviorCompleted = true;
            }
        }

        [Handler]
        public class TriggerMessageHandler : IHandleMessages<TriggerMessage>
        {
            public Task Handle(TriggerMessage message, IMessageHandlerContext context)
            {
                var options = new SendOptions();
                options.RouteToThisEndpoint();
                return context.Send(new FollowUpMessage(), options);
            }
        }

        [Handler]
        public class FollowUpMessageHandler(Context testContext) : IHandleMessages<FollowUpMessage>
        {
            public Task Handle(FollowUpMessage message, IMessageHandlerContext context)
            {
                testContext.FollowUpReceived = true;
                return Task.CompletedTask;
            }
        }
    }

    public class TriggerMessage : ICommand;

    public class FollowUpMessage : ICommand;
}
