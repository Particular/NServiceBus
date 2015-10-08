﻿namespace NServiceBus.AcceptanceTests.Reliability.Outbox
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using NServiceBus.Config;
    using NUnit.Framework;
    using Pipeline;

    public class When_dispatching_forwarded_messages : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_dispatched_immediately()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithAuditOn>(b => b.When(bus => bus.SendLocalAsync(new MessageToBeForwarded())))
                .WithEndpoint<ForwardingSpyEndpoint>()
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
                    .WithConfig<UnicastBusConfig>(c => c.ForwardReceivedMessagesTo = "forward_receiver_outbox");
            }

            class BlowUpAfterDispatchBehavior : Behavior<BatchDispatchContext>
            {
                static bool called;

                public override async Task Invoke(BatchDispatchContext context, Func<Task> next)
                {
                    if (!context.Operations.Any(op => op.Message.Headers[Headers.EnclosedMessageTypes].Contains(typeof(MessageToBeForwarded).Name)))
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
            }


            public class MessageToBeForwardedHandler : IHandleMessages<MessageToBeForwarded>
            {
                public Task Handle(MessageToBeForwarded message)
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
                public IBus Bus { get; set; }

                public Task Handle(MessageToBeForwarded message)
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