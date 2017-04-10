namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_message_is_deferred_by_delayed_retries_using_dtc : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_commit_distributed_transaction()
        {
            Requires.DtcSupport();

            var context = await Scenario.Define<Context>(c => c.Id = Guid.NewGuid())
                .WithEndpoint<Endpoint>(b => b.DoNotFailOnErrorMessages()
                    .When((session, c) => session.SendLocal(new MessageToFail
                    {
                        Id = c.Id
                    }))
                 )
                .Done(c => c.FailedMessages.Any())
                .Run();

            Assert.Greater(context.NumberOfProcessingAttempts, 1, "Should retry at least once");
            Assert.That(context.TransactionStatuses, Is.All.Not.EqualTo(TransactionStatus.Committed));
        }

        const string ErrorQueueName = "error_spy_queue";

        class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public List<TransactionStatus> TransactionStatuses { get; } = new List<TransactionStatus>();
            public int NumberOfProcessingAttempts { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.SendFailedMessagesTo(ErrorQueueName);
                    var recoverability = config.Recoverability();
                    recoverability.Delayed(settings =>
                    {
                        settings.NumberOfRetries(3);
                        settings.TimeIncrease(TimeSpan.FromSeconds(1));
                    });
                });
            }

            class FailingHandler : IHandleMessages<MessageToFail>
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageToFail message, IMessageHandlerContext context)
                {
                    if (message.Id == TestContext.Id)
                    {
                        TestContext.NumberOfProcessingAttempts++;

                        Transaction.Current.TransactionCompleted += CaptureTransactionStatus;
                    }

                    throw new SimulatedException();
                }

                void CaptureTransactionStatus(object sender, TransactionEventArgs args)
                {
                    TestContext.TransactionStatuses.Add(args.Transaction.TransactionInformation.Status);
                }
            }
        }

        public class MessageToFail : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}