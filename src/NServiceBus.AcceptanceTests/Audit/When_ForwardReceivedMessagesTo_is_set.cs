namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_ForwardReceivedMessagesTo_is_set : NServiceBusAcceptanceTest
    {
            [Test]
            public void Should_forward_message()
            {
                var context = new Context();

                Scenario.Define(context)
                    .WithEndpoint<EndpointThatForwards>(b => b.Given((bus, c) =>
                    {
                        bus.SendLocal(new MessageToForward());
                    }))
                    .WithEndpoint<ForwardReceiver>()
                    .Done(c => c.GotForwardedMessage)
                    .Run();

                Assert.IsTrue(context.GotForwardedMessage);
            }

            public class Context : ScenarioContext
            {
                public bool GotForwardedMessage { get; set; }
            }

            public class ForwardReceiver : EndpointConfigurationBuilder
            {
                public ForwardReceiver()
                {
                    EndpointSetup<DefaultServer>(c => c.EndpointName("forward_receiver"));
                }

                public class MessageToForwardHandler : IHandleMessages<MessageToForward>
                {
                    public Context Context { get; set; }

                    public void Handle(MessageToForward message)
                    {
                        Context.GotForwardedMessage = true;
                    }
                }
            }

            public class EndpointThatForwards : EndpointConfigurationBuilder
            {
                public EndpointThatForwards()
                {
                    EndpointSetup<DefaultServer>()
                        .WithConfig<UnicastBusConfig>(c => c.ForwardReceivedMessagesTo = "forward_receiver");
                }

                public class MessageToForwardHandler : IHandleMessages<MessageToForward>
                {
                    public void Handle(MessageToForward message)
                    {
                    }
                }
            }

            [Serializable]
            public class MessageToForward : IMessage
            {
            }
        }
    }
