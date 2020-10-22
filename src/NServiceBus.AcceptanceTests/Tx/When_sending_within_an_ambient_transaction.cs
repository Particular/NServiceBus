namespace NServiceBus.AcceptanceTests.Tx
{
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_within_an_ambient_transaction : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_deliver_them_until_the_commit_phase()
        {
            Requires.DtcSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<TransactionalEndpoint>(b => b.When(async (session, ctx) =>
                {
                    using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                    {
                        await session.Send(new MessageThatIsEnlisted
                        {
                            SequenceNumber = 1
                        });
                        await session.Send(new MessageThatIsEnlisted
                        {
                            SequenceNumber = 2
                        });

                        //send another message as well so that we can check the order in the receiver
                        using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                        {
                            await session.Send(new MessageThatIsNotEnlisted());
                        }

                        tx.Complete();
                    }
                }))
                .Done(c => c.MessageThatIsNotEnlistedHandlerWasCalled && c.TimesCalled >= 2)
                .Run();

            Assert.AreEqual(1, context.SequenceNumberOfFirstMessage, "The transport should preserve the order in which the transactional messages are delivered to the queuing system");
        }

        [Test]
        public async Task Should_not_deliver_them_on_rollback()
        {
            Requires.DtcSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<TransactionalEndpoint>(b => b.When(async session =>
                {
                    using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                    {
                        await session.Send(new MessageThatIsEnlisted());
                        //rollback
                    }

                    await session.Send(new MessageThatIsNotEnlisted());
                }))
                .Done(c => c.MessageThatIsNotEnlistedHandlerWasCalled)
                .Run();

            Assert.False(context.MessageThatIsEnlistedHandlerWasCalled, "The transactional handler should not be called");
        }

        public class Context : ScenarioContext
        {
            public bool MessageThatIsEnlistedHandlerWasCalled { get; set; }

            public bool MessageThatIsNotEnlistedHandlerWasCalled { get; set; }
            public int TimesCalled { get; set; }

            public int SequenceNumberOfFirstMessage { get; set; }

            public bool NonTransactionalHandlerCalledFirst { get; set; }
        }

        public class TransactionalEndpoint : EndpointConfigurationBuilder
        {
            public TransactionalEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.LimitMessageProcessingConcurrencyTo(1);
                    var routing = c.Routing();
                    routing.RouteToEndpoint(typeof(MessageThatIsEnlisted), typeof(TransactionalEndpoint));
                    routing.RouteToEndpoint(typeof(MessageThatIsNotEnlisted), typeof(TransactionalEndpoint));
                });
            }

            public class MessageThatIsEnlistedHandler : IHandleMessages<MessageThatIsEnlisted>
            {
                public MessageThatIsEnlistedHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageThatIsEnlisted messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    testContext.MessageThatIsEnlistedHandlerWasCalled = true;
                    testContext.TimesCalled++;

                    if (testContext.SequenceNumberOfFirstMessage == 0)
                    {
                        testContext.SequenceNumberOfFirstMessage = messageThatIsEnlisted.SequenceNumber;
                    }

                    return Task.FromResult(0);
                }

                Context testContext;
            }

            public class MessageThatIsNotEnlistedHandler : IHandleMessages<MessageThatIsNotEnlisted>
            {
                public MessageThatIsNotEnlistedHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageThatIsNotEnlisted messageThatIsNotEnlisted, IMessageHandlerContext context)
                {
                    testContext.MessageThatIsNotEnlistedHandlerWasCalled = true;
                    testContext.NonTransactionalHandlerCalledFirst = !testContext.MessageThatIsEnlistedHandlerWasCalled;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MessageThatIsEnlisted : ICommand
        {
            public int SequenceNumber { get; set; }
        }

        public class MessageThatIsNotEnlisted : ICommand
        {
        }
    }
}