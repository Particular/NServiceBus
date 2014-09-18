namespace NServiceBus.AcceptanceTests.Tx
{
    using System;
    using System.Transactions;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_sending_within_an_ambient_transaction : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_deliver_them_until_the_commit_phase()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<TransactionalEndpoint>(b => b.Given((bus, context) =>
                        {
                            using (var tx = new TransactionScope())
                            {
                                bus.Send(new MessageThatIsEnlisted { SequenceNumber = 1 });
                                bus.Send(new MessageThatIsEnlisted { SequenceNumber = 2 });

                                //send another message as well so that we can check the order in the receiver
                                using (new TransactionScope(TransactionScopeOption.Suppress))
                                {
                                    bus.Send(new MessageThatIsNotEnlisted());
                                }

                                tx.Complete();
                            }
                        }))
                    .Done(c => c.MessageThatIsNotEnlistedHandlerWasCalled && c.TimesCalled >= 2)
                    .Repeat(r => r.For<AllDtcTransports>())
                    .Should(c => Assert.AreEqual(1, c.SequenceNumberOfFirstMessage,"The transport should preserve the order in which the transactional messages are delivered to the queuing system"))
                    .Run();
        }

        [Test]
        public void Should_not_deliver_them_on_rollback()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<TransactionalEndpoint>(b => b.Given(bus =>
                        {
                            using (new TransactionScope())
                            {
                                bus.Send(new MessageThatIsEnlisted());

                                //rollback
                            }

                            bus.Send(new MessageThatIsNotEnlisted());

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
                EndpointSetup<DefaultServer>()
                    .AddMapping<MessageThatIsEnlisted>(typeof(TransactionalEndpoint))
                    .AddMapping<MessageThatIsNotEnlisted>(typeof(TransactionalEndpoint));
            }

            public class MessageThatIsEnlistedHandler : IHandleMessages<MessageThatIsEnlisted>
            {
                public Context Context { get; set; }

                public void Handle(MessageThatIsEnlisted messageThatIsEnlisted)
                {
                    Context.MessageThatIsEnlistedHandlerWasCalled = true;
                    Context.TimesCalled++;

                    if (Context.SequenceNumberOfFirstMessage == 0)
                    {
                        Context.SequenceNumberOfFirstMessage = messageThatIsEnlisted.SequenceNumber;
                    }
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
