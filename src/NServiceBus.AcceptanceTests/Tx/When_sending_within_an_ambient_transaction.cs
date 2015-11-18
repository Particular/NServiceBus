namespace NServiceBus.AcceptanceTests.Tx
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_sending_within_an_ambient_transaction : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_deliver_them_until_the_commit_phase()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<TransactionalEndpoint>(b => b.When(async (bus, context) =>
                        {
                            using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                            {
                                await bus.Send(new MessageThatIsEnlisted { SequenceNumber = 1 });
                                await bus.Send(new MessageThatIsEnlisted { SequenceNumber = 2 });

                                //send another message as well so that we can check the order in the receiver
                                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                                {
                                    await bus.Send(new MessageThatIsNotEnlisted());
                                }

                                tx.Complete();
                            }
                        }))
                    .Done(c => c.MessageThatIsNotEnlistedHandlerWasCalled && c.TimesCalled >= 2)
                    .Repeat(r => r.For<AllDtcTransports>())
                    .Should(c => Assert.AreEqual(1, c.SequenceNumberOfFirstMessage, "The transport should preserve the order in which the transactional messages are delivered to the queuing system"))
                    .Run();
        }

        [Test]
        public async Task Should_not_deliver_them_on_rollback()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<TransactionalEndpoint>(b => b.When(async bus =>
                        {
                            using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                            {
                                await bus.Send(new MessageThatIsEnlisted());
                                //rollback
                            }

                            await bus.Send(new MessageThatIsNotEnlisted());
                        }))
                    .Done(c => c.MessageThatIsNotEnlistedHandlerWasCalled)
                    .Repeat(r => r.For<AllDtcTransports>())
                    .Should(c => Assert.False(c.MessageThatIsEnlistedHandlerWasCalled, "The transactional handler should not be called"))
                    .Run();
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
                EndpointSetup<DefaultServer>(c => c.LimitMessageProcessingConcurrencyTo(1))
                    .AddMapping<MessageThatIsEnlisted>(typeof(TransactionalEndpoint))
                    .AddMapping<MessageThatIsNotEnlisted>(typeof(TransactionalEndpoint));
            }

            public class MessageThatIsEnlistedHandler : IHandleMessages<MessageThatIsEnlisted>
            {
                public Context Context { get; set; }

                public Task Handle(MessageThatIsEnlisted messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.MessageThatIsEnlistedHandlerWasCalled = true;
                    Context.TimesCalled++;

                    if (Context.SequenceNumberOfFirstMessage == 0)
                    {
                        Context.SequenceNumberOfFirstMessage = messageThatIsEnlisted.SequenceNumber;
                    }

                    return Task.FromResult(0);
                }
            }

            public class MessageThatIsNotEnlistedHandler : IHandleMessages<MessageThatIsNotEnlisted>
            {
                public Context Context { get; set; }

                public Task Handle(MessageThatIsNotEnlisted messageThatIsNotEnlisted, IMessageHandlerContext context)
                {
                    Context.MessageThatIsNotEnlistedHandlerWasCalled = true;
                    Context.NonTransactionalHandlerCalledFirst = !Context.MessageThatIsEnlistedHandlerWasCalled;
                    return Task.FromResult(0);
                }
            }
        }


        [Serializable]
        public class MessageThatIsEnlisted : ICommand
        {
            public int SequenceNumber { get; set; }
        }
        [Serializable]
        public class MessageThatIsNotEnlisted : ICommand
        {
        }


    }
}
