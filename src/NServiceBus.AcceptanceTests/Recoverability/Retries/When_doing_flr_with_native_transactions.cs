namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_doing_flr_with_native_transactions : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_do_5_retries_by_default_with_native_transactions()
        {
            await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<RetryEndpoint>(b => b.Given((bus, context) => bus.SendLocalAsync(new MessageToBeRetried { Id = context.Id })))
                    .AllowSimulatedExceptions()
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
                EndpointSetup<DefaultServer>(b =>
                {
                    b.Transactions().DisableDistributedTransactions();
                    b.DisableFeature<Features.SecondLevelRetries>();
                });
            }

            class ErrorNotificationSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public BusNotifications BusNotifications { get; set; }

                public Task StartAsync()
                {
                    BusNotifications.Errors.MessageSentToErrorQueue.Subscribe(e =>
                    {
                        Context.ForwardedToErrorQueue = true;
                    });
                    return Task.FromResult(0);
                }

                public Task StopAsync()
                {
                    return Task.FromResult(0);
                }
            }


            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public IBus Bus { get; set; }

                public Context Context { get; set; }

                public Task Handle(MessageToBeRetried message)
                {
                    if (message.Id != Context.Id)
                        return Task.FromResult(0); // messages from previous test runs must be ignored

                    Context.PhysicalMessageId = Bus.CurrentMessageContext.Id;
                    Context.NumberOfTimesInvoked++;

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