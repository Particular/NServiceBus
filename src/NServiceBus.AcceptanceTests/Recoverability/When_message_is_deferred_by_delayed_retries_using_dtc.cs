namespace NServiceBus.AcceptanceTests.Recoverability;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using EndpointTemplates;
using NUnit.Framework;

public class When_message_is_deferred_by_delayed_retries_using_dtc : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_not_commit_distributed_transaction()
    {
        Requires.DtcSupport();
        Requires.DelayedDelivery();

        var exception = Assert.ThrowsAsync<MessageFailedException>(async () => await Scenario.Define<Context>(c => c.Id = Guid.NewGuid())
            .WithEndpoint<Endpoint>(b => b
                .When((session, c) => session.SendLocal(new MessageToFail
                {
                    Id = c.Id
                }))
             )
            .Run());

        var context = (Context)exception.ScenarioContext;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.NumberOfRetriesAttempted, Is.GreaterThanOrEqualTo(3), "Should retry at least three times");
            Assert.That(context.TransactionStatuses, Is.All.Not.EqualTo(TransactionStatus.Committed));
        }
    }

    class Context : ScenarioContext
    {
        public Guid Id { get; set; }
        public List<TransactionStatus> TransactionStatuses { get; } = [];
        public int NumberOfProcessingAttempts { get; set; }
        public int NumberOfRetriesAttempted => NumberOfProcessingAttempts - 1 < 0 ? 0 : NumberOfProcessingAttempts - 1;
    }

    class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() =>
            EndpointSetup<DefaultServer>(config =>
            {
                var recoverability = config.Recoverability();
                recoverability.Delayed(settings =>
                {
                    settings.NumberOfRetries(3);
                    settings.TimeIncrease(TimeSpan.FromSeconds(1));
                });
            });

        class FailingHandler(Context testContext) : IHandleMessages<MessageToFail>
        {
            public Task Handle(MessageToFail message, IMessageHandlerContext context)
            {
                if (message.Id == testContext.Id)
                {
                    testContext.NumberOfProcessingAttempts++;

                    Transaction.Current.TransactionCompleted += CaptureTransactionStatus;
                }

                throw new SimulatedException();
            }

            void CaptureTransactionStatus(object sender, TransactionEventArgs args) => testContext.TransactionStatuses.Add(args.Transaction.TransactionInformation.Status);
        }
    }

    public class MessageToFail : IMessage
    {
        public Guid Id { get; set; }
    }
}