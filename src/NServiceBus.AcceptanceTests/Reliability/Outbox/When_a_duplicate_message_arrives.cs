namespace NServiceBus.AcceptanceTests.Reliability.Outbox
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_a_duplicate_message_arrives : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_dispatch_messages_already_dispatched()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<OutboxEndpoint>(b => b.When(async session =>
                {
                    var duplicateMessageId = Guid.NewGuid().ToString();

                    var options = new SendOptions();

                    options.SetMessageId(duplicateMessageId);
                    options.RouteToThisEndpoint();

                    await session.Send(new PlaceOrder(), options);
                    await session.Send(new PlaceOrder(), options);
                    await session.SendLocal(new PlaceOrder
                    {
                        Terminator = true
                    });
                }))
                .WithEndpoint<DownstreamEndpoint>()
                .Done(c => c.Done)
                .Repeat(r => r.For<AllOutboxCapableStorages>())
                .Should(context => Assert.AreEqual(2, context.MessagesReceivedByDownstreamEndpoint))
                .Run();
        }

        public class Context : ScenarioContext
        {
            public int MessagesReceivedByDownstreamEndpoint { get; set; }
            public bool Done { get; set; }
        }

        public class DownstreamEndpoint : EndpointConfigurationBuilder
        {
            public DownstreamEndpoint()
            {
                EndpointSetup<DefaultServer>(b => { b.LimitMessageProcessingConcurrencyTo(1); });
            }

            class SendOrderAcknowledgementHandler : IHandleMessages<SendOrderAcknowledgement>
            {
                public Context Context { get; set; }

                public Task Handle(SendOrderAcknowledgement message, IMessageHandlerContext context)
                {
                    Context.MessagesReceivedByDownstreamEndpoint++;
                    if (message.Terminator)
                    {
                        Context.Done = true;
                    }
                    return Task.FromResult(0);
                }
            }
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
                }).AddMapping<SendOrderAcknowledgement>(typeof(DownstreamEndpoint));
            }

            class PlaceOrderHandler : IHandleMessages<PlaceOrder>
            {
                public Task Handle(PlaceOrder message, IMessageHandlerContext context)
                {
                    return context.Send(new SendOrderAcknowledgement
                    {
                        Terminator = message.Terminator
                    });
                }
            }
        }

        class PlaceOrder : ICommand
        {
            public bool Terminator { get; set; }
        }

        class SendOrderAcknowledgement : IMessage
        {
            public bool Terminator { get; set; }
        }
    }
}