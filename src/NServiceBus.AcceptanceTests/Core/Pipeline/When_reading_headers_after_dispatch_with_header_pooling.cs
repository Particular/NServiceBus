namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_reading_headers_after_dispatch_with_header_pooling : NServiceBusAcceptanceTest
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Should_read_headers_after_next_in_outgoing_behavior(bool enableHeaderPooling)
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
                    return session.Send(new MyMessage(), options);
                }))
            .Done(c => c.MessageReceived && c.BehaviorCompleted)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.MessageReceived, Is.True, "The message must be dispatched.");
            Assert.That(context.ExceptionAfterNext, Is.Null, $"Reading headers after next() threw: {context.ExceptionAfterNext}");
            Assert.That(context.MessageIdAfterNext, Is.EqualTo(context.MessageIdBeforeNext));
        }
    }

    public class Context : ScenarioContext
    {
        public string MessageIdBeforeNext { get; set; }
        public string MessageIdAfterNext { get; set; }
        public Exception ExceptionAfterNext { get; set; }
        public bool BehaviorCompleted { get; set; }
        public bool MessageReceived { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>(c =>
            c.Pipeline.Register(b => new LogAfterSendBehavior(b.GetService<Context>()), "Reads the outgoing headers after the rest of the pipeline has run"));

        public class LogAfterSendBehavior(Context testContext) : Behavior<IOutgoingLogicalMessageContext>
        {
            public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
            {
                testContext.MessageIdBeforeNext = context.Headers[Headers.MessageId];

                await next();

                try
                {
                    testContext.MessageIdAfterNext = context.Headers[Headers.MessageId];
                }
                catch (KeyNotFoundException ex)
                {
                    testContext.ExceptionAfterNext = ex;
                }

                testContext.BehaviorCompleted = true;
            }
        }

        [Handler]
        public class MyMessageHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : ICommand;
}