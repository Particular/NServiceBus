namespace NServiceBus.AcceptanceTests.Forwarding
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_ForwardReceivedMessagesTo_is_set : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_forward_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointThatForwards>(b => b.When((bus, c) => bus.SendLocal(new MessageToForward())))
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

                public Task Handle(MessageToForward message, IMessageHandlerContext context)
                {
                    Context.GotForwardedMessage = true;

                    return Task.FromResult(0);
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
                public Task Handle(MessageToForward message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MessageToForward : IMessage
        {
        }
    }
}
