namespace NServiceBus.AcceptanceTests.Reliability.Outbox
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_dispatching_forwarded_messages : NServiceBusAcceptanceTest
    {

        [Test]
        public async Task Should_be_dispatched_immediately()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithAuditOn>(b => b
                        .When(bus => bus.SendLocal(new MessageToBeForwarded()))
                        .DoNotFailOnErrorMessages())
                    .WithEndpoint<ForwardingSpyEndpoint>()
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
                     .WithConfig<UnicastBusConfig>(c => c.ForwardReceivedMessagesTo = "forward_receiver_outbox");
            }

            class BlowUpAfterDispatchBehavior : Behavior<IBatchDispatchContext>
            {
                public async override Task Invoke(IBatchDispatchContext context, Func<Task> next)
                {
                    if (!context.Operations.Any(op => op.Message.Headers[Headers.EnclosedMessageTypes].Contains(typeof(MessageToBeForwarded).Name)))
                    {
                        await next().ConfigureAwait(false);
                        return;
                    }

                    await next().ConfigureAwait(false);

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
                EndpointSetup<DefaultServer>(c => c.EndpointName("forward_receiver_outbox"));
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeForwarded>
            {
                public Context Context { get; set; }

                public Task Handle(MessageToBeForwarded message, IMessageHandlerContext context)
                {
                    Context.Done = true;
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MessageToBeForwarded : IMessage
        {
            public string RunId { get; set; }
        }
    }
}