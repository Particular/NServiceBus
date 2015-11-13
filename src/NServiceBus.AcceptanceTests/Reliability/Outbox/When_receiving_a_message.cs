namespace NServiceBus.AcceptanceTests.Reliability.Outbox
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NUnit.Framework;

    public class When_receiving_a_message_not_found_in_the_outbox : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_handle_it()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<NonDtcReceivingEndpoint>(b => b.When(bus => bus.SendLocal(new PlaceOrder())))
                .Done(c => c.OrderAckReceived == 1)
                .Repeat(r => r.For<AllOutboxCapableStorages>())
                .Run(new RunSettings { TestExecutionTimeout = TimeSpan.FromSeconds(20) });
        }

        class Context : ScenarioContext
        {
            public int OrderAckReceived { get; set; }
        }

        public class NonDtcReceivingEndpoint : EndpointConfigurationBuilder
        {
            public NonDtcReceivingEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.GetSettings().Set("DisableOutboxTransportCheck", true);
                    b.EnableOutbox();
                });
            }

            class PlaceOrderHandler : IHandleMessages<PlaceOrder>
            {
                public Task Handle(PlaceOrder message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new SendOrderAcknowledgement());
                }
            }

            class SendOrderAcknowledgementHandler : IHandleMessages<SendOrderAcknowledgement>
            {
                public Context Context { get; set; }

                public Task Handle(SendOrderAcknowledgement message, IMessageHandlerContext context)
                {
                    Context.OrderAckReceived++;
                    return Task.FromResult(0);
                }
            }
        }

        class PlaceOrder : ICommand { }

        class SendOrderAcknowledgement : IMessage { }
    }
}
