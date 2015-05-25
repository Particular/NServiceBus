namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_error_is_overridden_in_code : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_error_to_target_queue()
        {
            var context = new Context();
            Scenario.Define(context)
                .WithEndpoint<UserEndpoint>(b => b.Given(bus => bus.SendLocal(new Message())))
                .WithEndpoint<ErrorSpy>()
                .AllowExceptions()
                .Done(c => c.MessageReceived)
                .Run();
        }

        public class UserEndpoint : EndpointConfigurationBuilder
        {
            public UserEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.DisableFeature<Features.SecondLevelRetries>();
                    b.SendFailedMessagesTo("error_with_code_source");
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            class Handler : IHandleMessages<Message>
            {
                public void Handle(Message message)
                {
                    throw new Exception();
                }
            }

        }

        public class ErrorSpy : EndpointConfigurationBuilder
        {
            public ErrorSpy()
            {
                EndpointSetup<DefaultServer>(c => c.EndpointName("error_with_code_source"));
            }

            class Handler : IHandleMessages<Message>
            {
                public Context MyContext { get; set; }

                public void Handle(Message message)
                {
                    MyContext.MessageReceived = true;
                }
            }
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }

        [Serializable]
        public class Message: IMessage
        {
        }

    }
}
