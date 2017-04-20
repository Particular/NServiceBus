namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
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
                .WithEndpoint<ErrorSpy>(b=>b.DoNotFailOnErrorMessages())
                .Done(c => c.MessageMovedToErrorQueue)
                .Run();

            Assert.That(context.TransactionStatuses, Is.All.Not.EqualTo(TransactionStatus.Committed));
        }

        const string ErrorQueueName = "error_spy_queue";

        class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool MessageMovedToErrorQueue { get; set; }
            public List<TransactionStatus> TransactionStatuses { get; } = new List<TransactionStatus>();
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.SendFailedMessagesTo(ErrorQueueName);
                });
            }

            class FailingHandler : IHandleMessages<MessageToFail>
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageToFail message, IMessageHandlerContext context)
                {
                    if (message.Id == TestContext.Id)
                    {
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

        class ErrorSpy : EndpointConfigurationBuilder
        {
            public ErrorSpy()
            {
                EndpointSetup<DefaultServer>().CustomEndpointName(ErrorQueueName);
            }

            class Handler : IHandleMessages<MessageToFail>
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageToFail message, IMessageHandlerContext context)
                {
                    if (message.Id == TestContext.Id)
                    {
                        TestContext.MessageMovedToErrorQueue = true;
                    }

                    return Task.FromResult(0);
                }
            }
        }

        public class MessageToFail : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}