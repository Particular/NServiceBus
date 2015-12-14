namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_doing_flr_with_default_settings : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_do_any_retries_if_transactions_are_off()
        {
            await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<RetryEndpoint>(b => b
                    .When(async (bus, context) =>
                    {
                        await bus.SendLocal(new MessageToBeRetried { Id = context.Id });
                        await bus.SendLocal(new MessageToBeRetried { Id = context.Id, SecondMessage = true });
                    })
                    .DoNotFailOnErrorMessages())
                .Done(c => c.SecondMessageReceived || c.NumberOfTimesInvoked > 1)
                .Repeat(r => r.For(Transports.Default))
                .Should(c => Assert.AreEqual(1, c.NumberOfTimesInvoked, "No retries should be in use if transactions are off"))
                .Run();
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public int NumberOfTimesInvoked { get; set; }

            public bool SecondMessageReceived { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.UseTransport(context.GetTransportType()).Transactions(TransportTransactionMode.None);
                });
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Context Context { get; set; }

                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    if (message.Id != Context.Id)
                    {
                        return Task.FromResult(0); // messages from previous test runs must be ignored
                    }

                    if (message.SecondMessage)
                    {
                        Context.SecondMessageReceived = true;
                        return Task.FromResult(0);
                    }

                    Context.NumberOfTimesInvoked++;

                    throw new SimulatedException();
                }
            }
        }

        [Serializable]
        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }

            public bool SecondMessage { get; set; }
        }
    }
}