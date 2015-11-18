namespace NServiceBus.AcceptanceTests.Reliability.Outbox
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NUnit.Framework;

    public class When_receiving_messages_already_dispatched : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_discard_them()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<OutboxEndpoint>(b => b.When(async bus =>
                    {
                        var duplicateMessageId = Guid.NewGuid().ToString();

                        var options = new SendOptions();

                        options.SetMessageId(duplicateMessageId);
                        options.RouteToLocalEndpointInstance();

                        await bus.Send(new PlaceOrder(), options);
                        await bus.Send(new PlaceOrder(), options);
                        await bus.SendLocal(new PlaceOrder());
                    }))
                    .Done(c => c.OrderAckReceived >= 2)
                    .Repeat(r => r.For<AllOutboxCapableStorages>())
                    .Should(context => Assert.AreEqual(2, context.OrderAckReceived))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public int OrderAckReceived { get; set; }
        }

        public class OutboxEndpoint : EndpointConfigurationBuilder
        {
            public OutboxEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.LimitMessageProcessingConcurrencyTo(1); // We limit to one to avoid race conditions on dispatch and this allows us to reliable check whether deduplication happens properly
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
