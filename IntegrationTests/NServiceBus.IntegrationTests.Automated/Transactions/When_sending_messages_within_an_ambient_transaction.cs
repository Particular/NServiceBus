namespace NServiceBus.IntegrationTests.Automated.Transactions
{
    using System;
    using System.Transactions;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;
    using Support;

    [TestFixture]
    public class When_sending_messages_within_an_ambient_transaction : NServiceBusIntegrationTest
    {
        [Test]
        public void Should_not_deliver_them_until_the_commit_phase()
        {
            Scenario.Define()
                    .WithEndpoint<TransactionalEndpoint>(() => new Context
                        {
                            WhenBehaviour = bus =>
                            {
                                using (var tx = new TransactionScope())
                                {
                                    bus.Send(new MessageThatIsEnlisted { SequenceNumber = 1 });
                                    bus.Send(new MessageThatIsEnlisted { SequenceNumber = 2 });

                                    //send another message as well so that we can check the order in the receiver
                                    using (new TransactionScope(TransactionScopeOption.Suppress))
                                        bus.Send(new MessageThatIsNotEnlisted());



                                    tx.Complete();
                                }
                            }
                        })
                     .Done<Context>(c => c.TimesCalled == 2)
                    .Repeat(r => r.For<AllTransports>())
                    .Should<Context>(c =>
                        {
                            Assert.True(c.MessageThatIsNotEnlistedHandlerWasCalled, "The non transactional handler should be called");
                            Assert.True(c.MessageThatIsEnlistedHandlerWasCalled, "The transactional handler should be called");
                            Assert.False(Context.NonTransactionalHandlerCalledFirst, "The handler for the transactional messages should have been called");
                            Assert.AreEqual(2, c.TimesCalled, "The transactional handler should be called twice");
                            Assert.AreEqual(1, c.SequenceNumberOfFirstMessage, "The transport should preserve the order in in which the transactional messages are delivered to the queuing system");

                        })

                    .Run();
        }

        [Test]
        public void Should_not_deliver_them_on_rollback()
        {
            Scenario.Define()
                    .WithEndpoint<TransactionalEndpoint>(() => new Context
                    {
                        WhenBehaviour = bus =>
                        {
                            using (new TransactionScope())
                            {
                                bus.Send(new MessageThatIsEnlisted { SequenceNumber = 1 });

                                //rollback
                            }

                            bus.Send(new MessageThatIsNotEnlisted());

                        }
                    })
                    .Done<Context>(c => c.MessageThatIsNotEnlistedHandlerWasCalled)
                    .Repeat(r => r.For<AllTransports>())
                    .Should<Context>(c =>
                    {
                        Assert.True(c.MessageThatIsNotEnlistedHandlerWasCalled, "The non transactional handler should be called");
                        Assert.False(c.MessageThatIsEnlistedHandlerWasCalled, "The transactional handler should not be called");
                    })
                    .Run();
        }

        public class Context : BehaviorContext
        {
            public bool MessageThatIsEnlistedHandlerWasCalled { get; set; }

            public bool MessageThatIsNotEnlistedHandlerWasCalled { get; set; }
            public int TimesCalled { get; set; }

            public int SequenceNumberOfFirstMessage { get; set; }

            public static bool NonTransactionalHandlerCalledFirst { get; set; }

            public Action<IBus> WhenBehaviour { get; set; }
        }

        public class TransactionalEndpoint : EndpointBuilder
        {
            public TransactionalEndpoint()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<MessageThatIsEnlisted>(typeof(TransactionalEndpoint))
                    .AddMapping<MessageThatIsNotEnlisted>(typeof(TransactionalEndpoint))
                    .When<Context>((bus, context) => context.WhenBehaviour(bus));
            }

            public class MessageThatIsEnlistedHandler : IHandleMessages<MessageThatIsEnlisted>
            {
                public Context Context { get; set; }

                public void Handle(MessageThatIsEnlisted messageThatIsEnlisted)
                {
                    Context.MessageThatIsEnlistedHandlerWasCalled = true;
                    Context.TimesCalled++;

                    if (Context.SequenceNumberOfFirstMessage == 0)
                        Context.SequenceNumberOfFirstMessage = messageThatIsEnlisted.SequenceNumber;
                }
            }

            public class MessageThatIsNotEnlistedHandler : IHandleMessages<MessageThatIsNotEnlisted>
            {
                public Context Context { get; set; }

                public void Handle(MessageThatIsNotEnlisted messageThatIsNotEnlisted)
                {
                    Context.MessageThatIsNotEnlistedHandlerWasCalled = true;
                    Context.NonTransactionalHandlerCalledFirst = !Context.MessageThatIsEnlistedHandlerWasCalled;
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
