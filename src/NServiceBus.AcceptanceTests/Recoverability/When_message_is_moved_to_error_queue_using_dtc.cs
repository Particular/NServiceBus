namespace NServiceBus.AcceptanceTests.Recoverability;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_message_is_moved_to_error_queue_using_dtc : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_commit_distributed_transaction()
    {
        Requires.DtcSupport();

        var context = await Scenario.Define<Context>(c => c.Id = Guid.NewGuid())
            .WithEndpoint<Endpoint>(b => b.DoNotFailOnErrorMessages()
                .When((session, ctx) => session.SendLocal(new MessageToFail
                {
                    Id = ctx.Id
                }))
            )
            .WithEndpoint<ErrorSpy>()
            .Run();

        Assert.That(context.TransactionStatuses, Is.All.Not.EqualTo(TransactionStatus.Committed));
    }

    public class Context : ScenarioContext
    {
        public Guid Id { get; set; }
        public bool MessageMovedToErrorQueue { get; set; }
        public List<TransactionStatus> TransactionStatuses { get; } = [];
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() =>
            EndpointSetup<DefaultServer>(config =>
            {
                config.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(ErrorSpy)));
            });

        [Handler]
        public class FailingHandler(Context testContext) : IHandleMessages<MessageToFail>
        {
            public Task Handle(MessageToFail message, IMessageHandlerContext context)
            {
                if (message.Id == testContext.Id)
                {
                    Transaction.Current.TransactionCompleted += CaptureTransactionStatus;
                }

                throw new SimulatedException();
            }

            void CaptureTransactionStatus(object sender, TransactionEventArgs args) => testContext.TransactionStatuses.Add(args.Transaction.TransactionInformation.Status);
        }
    }

    public class ErrorSpy : EndpointConfigurationBuilder
    {
        public ErrorSpy() => EndpointSetup<DefaultServer>();

        [Handler]
        public class Handler(Context testContext) : IHandleMessages<MessageToFail>
        {
            public Task Handle(MessageToFail message, IMessageHandlerContext context)
            {
                if (message.Id == testContext.Id)
                {
                    testContext.MessageMovedToErrorQueue = true;
                    testContext.MarkAsCompleted();
                }

                return Task.CompletedTask;
            }
        }
    }

    public class MessageToFail : IMessage
    {
        public Guid Id { get; set; }
    }
}