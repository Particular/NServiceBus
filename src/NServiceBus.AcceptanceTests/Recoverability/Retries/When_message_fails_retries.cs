namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_message_fails_retries : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_forward_message_to_error_queue()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<RetryEndpoint>(b => b.Given((bus, c) =>
                    {
                        bus.SendLocal(new MessageWhichFailsRetries());
                        return Task.FromResult(0);
                    }))
                    .AllowExceptions(e => e is RetryEndpoint.SimulatedException)
                    .Done(c => c.ForwardedToErrorQueue)
                    .Run();

            Assert.AreEqual(1, context.Logs.Count(l => l.Message
                .StartsWith(string.Format("Moving message '{0}' to the error queue because processing failed due to an exception:", context.PhysicalMessageId))));
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(configure =>
                {
                    configure.DisableFeature<FirstLevelRetries>();
                    configure.DisableFeature<SecondLevelRetries>();
                });
            }

            public static byte Checksum(byte[] data)
            {
                var longSum = data.Sum(x => (long)x);
                return unchecked((byte)longSum);
            }

            class ErrorNotificationSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public BusNotifications BusNotifications { get; set; }

                public void Start()
                {
                    BusNotifications.Errors.MessageSentToErrorQueue.Subscribe(e =>
                    {
                        Context.ForwardedToErrorQueue = true;
                    });
                }

                public void Stop()
                {
                }
            }

            class MessageHandler : IHandleMessages<MessageWhichFailsRetries>
            {
                public IBus Bus { get; set; }

                public Context Context { get; set; }

                public void Handle(MessageWhichFailsRetries message)
                {
                    Context.PhysicalMessageId = Bus.CurrentMessageContext.Id;
                    throw new SimulatedException();
                }
            }

            public class SimulatedException : Exception
            {
            }
        }

        public class Context : ScenarioContext
        {
            public bool ForwardedToErrorQueue { get; set; }

            public string PhysicalMessageId { get; set; }
        }

        public class MessageWhichFailsRetries : IMessage
        {
        }
    }
}