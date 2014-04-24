namespace NServiceBus.AcceptanceTests.NonDTCOperations
{
    using System;
    using Config;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Outbox;
    using Persistence.InMemory.Outbox;

    public class When_receiving_a_message : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_handle_it()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<NonDtcReceivingEndpoint>(b => b.Given(bus => bus.SendLocal(new PlaceOrder())))
                    .Done(c => context.TimesCalled == 1)
                    .Run();
        }


        [Test]
        public void Should_discard_duplicates_using_the_outbox()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<NonDtcReceivingEndpoint>(b => b.Given(bus =>
                    {
                        var duplicateMessageId = Guid.NewGuid().ToString();
                        bus.SendLocal<PlaceOrder>(m =>
                        {
                            m.SetHeader(Headers.MessageId, duplicateMessageId);
                            m.OrderId = duplicateMessageId;
                            m.Duplicate = true;
                        });
                        bus.SendLocal<PlaceOrder>(m =>
                        {
                            m.SetHeader(Headers.MessageId, duplicateMessageId);
                            m.OrderId = duplicateMessageId;
                            m.Duplicate = true;
                        });
                      
                        bus.SendLocal(new PlaceOrder());
                    }))
                    .Done(c => context.DuplicateMessageProcessed && context.NonDuplicateProcessed)
                    .Run();

            Assert.AreEqual(2,context.TimesCalled);
        }


        public class Context : ScenarioContext
        {
            public int TimesCalled { get; set; }
            public bool DuplicateMessageProcessed { get; set; }
            public bool NonDuplicateProcessed { get; set; }
        }


        public class NonDtcReceivingEndpoint : EndpointConfigurationBuilder
        {
            public NonDtcReceivingEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    InfrastructureServices.SetDefaultFor<IOutboxStorage>(() => 
                        Configure.Component<InMemoryOutboxStorage>(DependencyLifecycle.SingleInstance));

                    Configure.Transactions.Advanced(t =>
                    {
                        t.DisableDistributedTransactions();
                        t.DoNotWrapHandlersExecutionInATransactionScope();
                    });

                    Configure.Features.Enable<Features.Outbox>();
                });
            }

            class PlaceOrderHandler:IHandleMessages<PlaceOrder>
            {
                public Context Context { get; set; }
         
                public void Handle(PlaceOrder message)
                {
                    Context.TimesCalled++;
                    if (message.Duplicate)
                    {
                        Assert.False(Context.DuplicateMessageProcessed);
                        Context.DuplicateMessageProcessed = true;
                    }
                    else
                    {
                        Context.NonDuplicateProcessed = true;
                    }
                }
            }
          
        }

        [Serializable]
        public class PlaceOrder : ICommand
        {
            public string OrderId{ get; set; }
            public bool Duplicate { get; set; }
        }


    }

}
