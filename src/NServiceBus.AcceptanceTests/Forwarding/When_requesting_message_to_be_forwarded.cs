namespace NServiceBus.AcceptanceTests.Forwarding
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_requesting_message_to_be_forwarded : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_forward_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointThatForwards>(b => b.When((session, c) => session.SendLocal(new MessageToForward())))
                .WithEndpoint<ForwardReceiver>()
                .Done(c => c.GotForwardedMessage)
                .Run();

            Assert.IsTrue(context.GotForwardedMessage);
            CollectionAssert.AreEqual(context.ForwardedHeaders, context.ReceivedHeaders, "Headers should be preserved on the forwarded message");
        }

        public class Context : ScenarioContext
        {
            public bool GotForwardedMessage { get; set; }
            public IReadOnlyDictionary<string, string> ForwardedHeaders { get; set; }
            public IReadOnlyDictionary<string, string> ReceivedHeaders { get; set; }
        }

        public class ForwardReceiver : EndpointConfigurationBuilder
        {
            public ForwardReceiver()
            {
                EndpointSetup<DefaultServer>()
                    .CustomEndpointName("message_forward_receiver");
            }

            public class MessageToForwardHandler : IHandleMessages<MessageToForward>
            {
                public Context Context { get; set; }

                public MessageToForwardHandler(Context context)
                {
                    Context = context;
                }

                public Task Handle(MessageToForward message, IMessageHandlerContext context)
                {
                    Context.ForwardedHeaders = context.MessageHeaders;
                    Context.GotForwardedMessage = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class EndpointThatForwards : EndpointConfigurationBuilder
        {
            public EndpointThatForwards()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MessageToForwardHandler : IHandleMessages<MessageToForward>
            {
                public Context Context { get; set; }

                public MessageToForwardHandler(Context context)
                {
                    Context = context;
                }

                public Task Handle(MessageToForward message, IMessageHandlerContext context)
                {
                    Context.ReceivedHeaders = context.MessageHeaders.ToDictionary(x => x.Key, x => x.Value);
                    return context.ForwardCurrentMessageTo("message_forward_receiver");
                }
            }
        }

        public class MessageToForward : IMessage
        {
        }
    }
}