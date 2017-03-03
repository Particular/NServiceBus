namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_immediate_retries_with_default_settings : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_do_any_retries_if_transactions_are_off()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<RetryEndpoint>(b => b
                    .When(async (session, ctx) =>
                    {
                        await session.SendLocal(new MessageToBeRetried
                        {
                            Id = ctx.Id
                        });
                        await session.SendLocal(new MessageToBeRetried
                        {
                            Id = ctx.Id,
                            SecondMessage = true
                        });
                    })
                    .DoNotFailOnErrorMessages())
                .Done(c => c.SecondMessageReceived || c.NumberOfTimesInvoked > 1)
                .Run();

            Assert.AreEqual(1, context.NumberOfTimesInvoked, "No retries should be in use if transactions are off");
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
                EndpointSetup<DefaultServer>((config, context) => { config.ConfigureTransport().Transactions(TransportTransactionMode.None); });
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

        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }

            public bool SecondMessage { get; set; }
        }
    }
}