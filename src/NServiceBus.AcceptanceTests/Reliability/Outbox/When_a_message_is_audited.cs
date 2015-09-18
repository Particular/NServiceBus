namespace NServiceBus.AcceptanceTests.Reliability.Outbox
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Configuration.AdvanceExtensibility;
    using Pipeline;
    using NUnit.Framework;

    public class When_a_message_is_audited : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_dispatched_immediately()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithAuditOn>(b => b.When(bus => bus.SendLocalAsync(new MessageToBeAudited())))
                    .WithEndpoint<AuditSpyEndpoint>()
                    .AllowSimulatedExceptions()
                    .Done(c => c.Done)
                    .Run();

            Assert.True(context.Done);
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
        }

        public class EndpointWithAuditOn : EndpointConfigurationBuilder
        {
            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>(
                    b =>
                    {
                        b.GetSettings().Set("DisableOutboxTransportCheck", true);
                        b.EnableOutbox();
                        b.Pipeline.Register("BlowUpAfterDispatchBehavior", typeof(BlowUpAfterDispatchBehavior), "For testing");
                    })
                    .AuditTo<AuditSpyEndpoint>();
            }

            class BlowUpAfterDispatchBehavior : Behavior<BatchDispatchContext>
            {
                public async override Task Invoke(BatchDispatchContext context, Func<Task> next)
                {
                    if (!context.Operations.Any(op => op.Message.Headers[Headers.EnclosedMessageTypes].Contains(typeof(MessageToBeAudited).Name)))
                    {
                        await next().ConfigureAwait(false);
                        return;
                    }

                    if (called)
                    {
                        Console.Out.WriteLine("Called once, skipping next");
                        return;

                    }
                    await next().ConfigureAwait(false);

                    called = true;

                    throw new SimulatedException();
                }

                static bool called;
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Task Handle(MessageToBeAudited message)
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
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public Task Handle(MessageToBeAudited message)
                {
                    Context.Done = true;
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MessageToBeAudited : IMessage
        {
            public string RunId { get; set; }
        }
    }
}