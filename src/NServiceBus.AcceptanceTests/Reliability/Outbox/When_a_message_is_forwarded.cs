namespace NServiceBus.AcceptanceTests.Reliability.Outbox
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_a_message_is_forwarding : NServiceBusAcceptanceTest
    {

        [Test]
        public async Task Should_forward_even_if_dispatch_blows_once()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithAuditOn>(b => b.Given(bus =>
                    {
                        bus.SendLocal(new MessageToBeForwarded());
                        return Task.FromResult(0);
                    }))
                    .WithEndpoint<ForwardingSpyEndpoint>()
                    .AllowExceptions(e => e is EndpointWithAuditOn.BlowUpAfterDispatchBehavior.FakeException)
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
                        b.Pipeline.Register<BlowUpAfterDispatchBehavior.Registration>();
                    })
                     .WithConfig<UnicastBusConfig>(c => c.ForwardReceivedMessagesTo = "forward_receiver_outbox");
            }

            public class BlowUpAfterDispatchBehavior : PhysicalMessageProcessingStageBehavior
            {
                public class Registration : RegisterStep
                {
                    public Registration()
                        : base("BlowUpAfterDispatchBehavior", typeof(BlowUpAfterDispatchBehavior), "For testing")
                    {
                        InsertAfter("FirstLevelRetries");
                        InsertBefore("InvokeForwardingPipeline");
                    }
                }

                public override void Invoke(Context context, Action next)
                {
                    if (!context.GetPhysicalMessage().Headers[Headers.EnclosedMessageTypes].Contains(typeof(MessageToBeForwarded).Name))
                    {
                        next();
                        return;
                    }


                    if (called)
                    {
                        Console.Out.WriteLine("Called once, skipping next");
                        return;

                    }
                    else
                    {
                        next();
                    }


                    called = true;

                    throw new FakeException();
                }

                public class FakeException : Exception
                {
                }

                static bool called;
            }


            public class MessageToBeForwardedHandler : IHandleMessages<MessageToBeForwarded>
            {
                public void Handle(MessageToBeForwarded message)
                {
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

                public void Handle(MessageToBeForwarded message)
                {
                    Context.Done = true;
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
