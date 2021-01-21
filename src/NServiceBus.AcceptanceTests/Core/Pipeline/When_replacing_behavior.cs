namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading;
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
                .Done(c => c.MessageHandled)
                .Run();

            Assert.IsFalse(context.OriginalBehaviorInvoked);
            Assert.IsTrue(context.ReplacementBehaviorInvoked);
        }

        class Context : ScenarioContext
        {
            public bool OriginalBehaviorInvoked { get; set; }
            public bool ReplacementBehaviorInvoked { get; set; }

            public bool MessageHandled { get; set; }
        }

        class OriginalBehavior : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
        {
            public OriginalBehavior(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, CancellationToken, Task> next, CancellationToken token)
            {
                testContext.OriginalBehaviorInvoked = true;
                return next(context, token);
            }

            Context testContext;
        }

        class ReplacementBehavior : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
        {
            public ReplacementBehavior(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, CancellationToken, Task> next, CancellationToken token)
            {
                testContext.ReplacementBehaviorInvoked = true;
                return next(context, token);
            }

            Context testContext;
        }

        class EndpointWithReplacement : EndpointConfigurationBuilder
        {
            public EndpointWithReplacement()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    // replace before register to ensure out-of-order replacements work correctly.
                    c.Pipeline.Replace("demoBehavior", new ReplacementBehavior((Context)r.ScenarioContext));
                    c.Pipeline.Register("demoBehavior", new OriginalBehavior((Context)r.ScenarioContext), "test behavior replacement");
                });
            }

            public class Handler : IHandleMessages<Message>
            {
                public Handler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    testContext.MessageHandled = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class Message : IMessage
        {
        }
    }
}