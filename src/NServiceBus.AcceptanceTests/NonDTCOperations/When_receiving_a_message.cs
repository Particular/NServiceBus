namespace NServiceBus.AcceptanceTests.Sagas
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
                    .WithEndpoint<NonDtcSalesEndpoint>(b => b.Given(bus => bus.SendLocal(new PlaceOrder())))
                    .Done(c => context.HandlerCalled)
                    .Run();
        }



        public class Context : ScenarioContext
        {
            public bool HandlerCalled { get; set; }
        }


        public class NonDtcSalesEndpoint : EndpointConfigurationBuilder
        {
            public NonDtcSalesEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    InfrastructureServices.SetDefaultFor<IOutboxStorage>(() => 
                        Configure.Component<InMemoryOutboxStorage>(DependencyLifecycle.SingleInstance));
           
                    Configure.Features.Enable<Features.Outbox>();
                });
            }

            class PlaceOrderHandler:IHandleMessages<PlaceOrder>
            {
                public Context Context { get; set; }
               
                public void Handle(PlaceOrder message)
                {
                    Context.HandlerCalled = true;
                }
            }
          
        }

        [Serializable]
        public class PlaceOrder : ICommand
        {
        }


    }

}
