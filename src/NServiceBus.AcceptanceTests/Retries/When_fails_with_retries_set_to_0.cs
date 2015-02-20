namespace NServiceBus.AcceptanceTests.Retries
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_fails_with_retries_set_to_0 : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_retry_the_message_using_flr()
        {
            var context = new Context()
            {
                Id = Guid.NewGuid()
            };

            Scenario.Define(context)
                    .WithEndpoint<RetryEndpoint>(b => b.Given((bus, ctx) => bus.SendLocal(new MessageToBeRetried(){ContextId = ctx.Id})))
                    .AllowExceptions()
                    .Done(c => c.GaveUp)
                    .Run();

            Assert.AreEqual(1, context.NumberOfTimesInvoked,"No FLR should be in use if MaxRetries is set to 0");
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public int NumberOfTimesInvoked { get; set; }

            public bool GaveUp { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(b => b.DisableFeature<Features.SecondLevelRetries>())
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            class ErrorNotificationSpy: IWantToRunWhenBusStartsAndStops
            {
                public Context  Context { get; set; }

                public BusNotifications BusNotifications { get; set; }

                public void Start()
                {
                    BusNotifications.Errors.MessageSentToErrorQueue.Subscribe(e =>
                    {
                        Context.GaveUp = true;
                    });
                }

                public void Stop(){}
            }

            class MessageToBeRetriedHandler:IHandleMessages<MessageToBeRetried>
            {
                public Context Context { get; set; }
                public void Handle(MessageToBeRetried message)
                {
                    if (Context.Id != message.ContextId)
                    {
                        return;
                    }
                    Context.NumberOfTimesInvoked++;
                    throw new Exception("Simulated exception");
                }
            }
        }

        [Serializable]
        public class MessageToBeRetried : IMessage
        {
            public Guid ContextId { get; set; }
        }
    }


}