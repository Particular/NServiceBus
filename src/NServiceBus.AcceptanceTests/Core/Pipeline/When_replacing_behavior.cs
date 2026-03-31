namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_replacing_behavior : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_invoke_replacement_in_pipeline()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithReplacement>(e => e
                .When(s => s.SendLocal<Message>(m => { })))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.OriginalBehaviorInvoked, Is.False);
            Assert.That(context.ReplacementBehaviorInvoked, Is.True);
        }
    }

    public class Context : ScenarioContext
    {
        public bool OriginalBehaviorInvoked { get; set; }
        public bool ReplacementBehaviorInvoked { get; set; }
    }

    class OriginalBehavior(Context testContext) : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
    {
        public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
        {
            testContext.OriginalBehaviorInvoked = true;
            return next(context);
        }
    }

    class ReplacementBehavior(Context testContext) : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
    {
        public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
        {
            testContext.ReplacementBehaviorInvoked = true;
            return next(context);
        }
    }

    public class EndpointWithReplacement : EndpointConfigurationBuilder
    {
        public EndpointWithReplacement() =>
            EndpointSetup<DefaultServer>((c, r) =>
            {
                // replace before register to ensure out-of-order replacements work correctly.
                c.Pipeline.Replace("demoBehavior", new ReplacementBehavior((Context)r.ScenarioContext));
                c.Pipeline.Register("demoBehavior", new OriginalBehavior((Context)r.ScenarioContext), "test behavior replacement");
            });

        [Handler]
        public class Handler(Context testContext) : IHandleMessages<Message>
        {
            public Task Handle(Message message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class Message : IMessage;
}