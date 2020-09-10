namespace NServiceBus.AcceptanceTests.Outbox
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_a_message_is_audited : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_dispatch_audit_message_immediately()
        {
            Requires.OutboxPersistence();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithAuditOn>(b => b
                    .When(session => session.SendLocal(new MessageToBeAudited()))
                    .DoNotFailOnErrorMessages())
                .WithEndpoint<AuditSpyEndpoint>()
                .Done(c => c.MessageAudited)
                .Run();

            Assert.True(context.MessageAudited);
        }

       class Context : ScenarioContext
        {
            public bool MessageAudited { get; set; }
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
                        b.AuditProcessedMessagesTo<AuditSpyEndpoint>();
                    });
            }

            class BlowUpAfterDispatchBehavior : IBehavior<IBatchDispatchContext, IBatchDispatchContext>
            {
                public async Task Invoke(IBatchDispatchContext context, Func<IBatchDispatchContext, CancellationToken, Task> next, CancellationToken cancellationToken)
                {
                    if (!context.Operations.Any(op => op.Message.Headers[Headers.EnclosedMessageTypes].Contains(typeof(MessageToBeAudited).Name)))
                    {
                        await next(context, cancellationToken).ConfigureAwait(false);
                        return;
                    }

                    await next(context, cancellationToken).ConfigureAwait(false);

                    throw new SimulatedException();
                }
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    return Task.FromResult(0);
                }
            }
        }

        class AuditSpyEndpoint : EndpointConfigurationBuilder
        {
            public AuditSpyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public MessageToBeAuditedHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.MessageAudited = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }


        public class MessageToBeAudited : IMessage
        {
            public string RunId { get; set; }
        }
    }
}