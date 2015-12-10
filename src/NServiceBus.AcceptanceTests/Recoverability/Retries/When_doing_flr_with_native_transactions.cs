namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_doing_flr_with_native_transactions : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_do_5_retries_by_default_with_native_transactions()
        {
            await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<RetryEndpoint>(b => b
                        .When((bus, context) => bus.SendLocal(new MessageToBeRetried { Id = context.Id }))
                        .DoNotFailOnErrorMessages())
                    .Done(c => c.ForwardedToErrorQueue)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c =>
                    {
                        Assert.AreEqual(5 + 1, c.NumberOfTimesInvoked, "The FLR should by default retry 5 times");
                        Assert.AreEqual(5, c.Logs.Count(l => l.Message
                            .StartsWith($"First Level Retry is going to retry message '{c.PhysicalMessageId}' because of an exception:")));
                        Assert.AreEqual(1, c.Logs.Count(l => l.Message
                            .StartsWith($"Giving up First Level Retries for message '{c.PhysicalMessageId}'.")));
                    })
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public int NumberOfTimesInvoked { get; set; }

            public bool ForwardedToErrorQueue { get; set; }

            public string PhysicalMessageId { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.EnableFeature<FirstLevelRetries>();
                    config.UseTransport(context.GetTransportType())
                            .Transactions(TransportTransactionMode.ReceiveOnly);
                });
            }

            class ErrorNotificationSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public BusNotifications BusNotifications { get; set; }

                public Task Start(IBusSession session)
                {
                    BusNotifications.Errors.MessageSentToErrorQueue += (sender, message) => Context.ForwardedToErrorQueue = true;
                    return Task.FromResult(0);
                }

                public Task Stop(IBusSession session)
                {
                    return Task.FromResult(0);
                }
            }


            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    if (message.Id != TestContext.Id)
                        return Task.FromResult(0); // messages from previous test runs must be ignored

                    TestContext.PhysicalMessageId = context.MessageId;
                    TestContext.NumberOfTimesInvoked++;

                    throw new SimulatedException();
                }
            }
        }

        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }
        }
    }


}