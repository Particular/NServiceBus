namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_error_is_overridden_in_code : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_error_to_target_queue()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<UserEndpoint>(b => b
                    .When(session => session.SendLocal(new Message()))
                    .DoNotFailOnErrorMessages())
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.MessageReceived)
                .Run();

            Assert.True(context.MessageReceived);
        }

        public class UserEndpoint : EndpointConfigurationBuilder
        {
            public UserEndpoint()
            {
                EndpointSetup<DefaultServer>(b => { b.SendFailedMessagesTo("error_with_code_source"); });
            }

            class Handler : IHandleMessages<Message>
            {
                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    throw new SimulatedException();
                }
            }
        }

        public class ErrorSpy : EndpointConfigurationBuilder
        {
            public ErrorSpy()
            {
                EndpointSetup<DefaultServer>()
                    .CustomEndpointName("error_with_code_source");
            }

            class Handler : IHandleMessages<Message>
            {
                public Context MyContext { get; set; }

                public Handler(Context myContext)
                {
                    MyContext = myContext;
                }

                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    MyContext.MessageReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }


        public class Message : IMessage
        {
        }
    }
}