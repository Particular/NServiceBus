namespace NServiceBus.AcceptanceTests.Core.Conventions
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_receiving_non_matching_message_with_handler : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_process_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(c => c.When(s => s.Send(new NonMatchingMessageWithHandler())))
                .WithEndpoint<Receiver>()
                .Done(c => c.GotTheMessage)
                .Run();

            Assert.True(context.GotTheMessage);
        }

        public class Context : ScenarioContext
        {
            public bool GotTheMessage { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.ConfigureRouting().RouteToEndpoint(typeof(NonMatchingMessageWithHandler), typeof(Receiver));
                });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c => c.Conventions().DefiningMessagesAs(t => false));
            }

            class MyHandler : IHandleMessages<NonMatchingMessageWithHandler>
            {
                public MyHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(NonMatchingMessageWithHandler message, IMessageHandlerContext context)
                {
                    testContext.GotTheMessage = true;
                    return Task.FromResult(0);
                }

                readonly Context testContext;
            }
        }

        public class NonMatchingMessageWithHandler : IMessage
        {
        }
    }
}