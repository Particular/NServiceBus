namespace NServiceBus.AcceptanceTests.Reliability.Outbox
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_dispatching_forwarded_messages : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_be_dispatched_immediately()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<EndpointWithAuditOn>(b => b
                    .When(session => session.SendLocal(new MessageToBeForwarded()))
                    .DoNotFailOnErrorMessages())
                .WithEndpoint<ForwardingSpyEndpoint>()
                .Done(c => c.Done)
                .Repeat(r => r.For<AllOutboxCapableStorages>())
                .Should(c => Assert.IsTrue(c.Done))
                .Run();
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
                        b.ForwardReceivedMessagesTo("forward_receiver_outbox");
                    });
            }

            class BlowUpAfterDispatchBehavior : Behavior<IBatchDispatchContext>
            {
                public override async Task Invoke(IBatchDispatchContext context, Func<Task> next)
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
                EndpointSetup<DefaultServer>()
                    .CustomEndpointName("forward_receiver_outbox");
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