﻿namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_transactions_off : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_do_any_retries()
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
                .Done(c => c.SecondMessageReceived && c.NumberOfTimesInvoked >= 1)
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
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.ConfigureTransport().Transactions(TransportTransactionMode.None);
                    var recoverability = config.Recoverability();
                    recoverability.Immediate(i => i.NumberOfRetries(3));
                    recoverability.Delayed(d => d.NumberOfRetries(3));
                });
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public MessageToBeRetriedHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    if (message.Id != testContext.Id)
                    {
                        return Task.FromResult(0); // messages from previous test runs must be ignored
                    }

                    if (message.SecondMessage)
                    {
                        testContext.SecondMessageReceived = true;
                        return Task.FromResult(0);
                    }

                    testContext.NumberOfTimesInvoked++;

                    throw new SimulatedException();
                }

                Context testContext;
            }
        }

        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }

            public bool SecondMessage { get; set; }
        }
    }
}