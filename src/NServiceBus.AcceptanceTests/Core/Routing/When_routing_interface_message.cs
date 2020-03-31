namespace NServiceBus.AcceptanceTests.Core.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using AcceptanceTesting.Customization;
    using NUnit.Framework;

    public class When_routing_interface_message : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_interface_types_route()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(c => c.When(b => b.SendLocal(new StartMessage())))
                .Done(c => c.GotTheMessage)
                .Run();

            Assert.True(context.GotTheMessage);
        }

        public class Context : ScenarioContext
        {
            public bool GotTheMessage { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    var routing = c.ConfigureTransport().Routing();
                    routing.RouteToEndpoint(typeof(IMyMessage), typeof(Endpoint));
                });
            }

            public class StartMessageHandler : IHandleMessages<StartMessage>
            {
                public IMessageCreator MessageCreator { get; set; }

                public StartMessageHandler(IMessageCreator messageCreator)
                {
                    MessageCreator = messageCreator;
                }

                public Task Handle(StartMessage message, IMessageHandlerContext context)
                {
                    var interfaceMessage = MessageCreator.CreateInstance<IMyMessage>();
                    return context.Send(interfaceMessage);
                }
            }

            public class IMyMessageHandler : IHandleMessages<IMyMessage>
            {
                public Context Context { get; set; }

                public IMyMessageHandler(Context context)
                {
                    Context = context;
                }

                public Task Handle(IMyMessage message, IMessageHandlerContext context)
                {
                    Context.GotTheMessage = true;

                    return Task.FromResult(0);
                }
            }
        }

        public class StartMessage : IMessage
        {
        }

        public interface IMyMessage : IMessage
        {
        }
    }
}
