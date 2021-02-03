namespace NServiceBus.AcceptanceTests.Outbox
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_dispatching_forwarded_messages : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_dispatched_immediately()
        {
            Requires.OutboxPersistence();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithAuditOn>(b => b
                    .When(session => session.SendLocal(new MessageToBeForwarded()))
                    .DoNotFailOnErrorMessages())
                .WithEndpoint<ForwardingSpyEndpoint>()
                .Done(c => c.Done)
                .Run();

            Assert.IsTrue(context.Done);
        }

        class Context : ScenarioContext
        {
            public bool Done { get; set; }
        }

        class EndpointWithAuditOn : EndpointConfigurationBuilder
        {
            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>(
                    b =>
                    {
                        b.EnableOutbox();
                        b.Pipeline.Register("BlowUpAfterDispatchBehavior", new BlowUpAfterDispatchBehavior(), "For testing");
#pragma warning disable 618
                        b.ForwardReceivedMessagesTo(Conventions.EndpointNamingConvention(typeof(ForwardingSpyEndpoint)));
#pragma warning restore 618
                    });
            }

            class BlowUpAfterDispatchBehavior : IBehavior<IBatchDispatchContext, IBatchDispatchContext>
            {
                public async Task Invoke(IBatchDispatchContext context, Func<IBatchDispatchContext, Task> next)
                {
                    if (!context.Operations.Any(op => op.Message.Headers[Headers.EnclosedMessageTypes].Contains(nameof(When_dispatching_forwarded_messages.MessageToBeForwarded))))
                    {
                        await next(context).ConfigureAwait(false);
                        return;
                    }

                    await next(context).ConfigureAwait(false);

                    throw new SimulatedException();
                }
            }

            public class MessageToBeForwardedHandler : IHandleMessages<MessageToBeForwarded>
            {
                public Task Handle(MessageToBeForwarded message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        class ForwardingSpyEndpoint : EndpointConfigurationBuilder
        {
            public ForwardingSpyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeForwarded>
            {
                public MessageToBeAuditedHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageToBeForwarded message, IMessageHandlerContext context)
                {
                    testContext.Done = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MessageToBeForwarded : IMessage
        {
            public string RunId { get; set; }
        }
    }
}