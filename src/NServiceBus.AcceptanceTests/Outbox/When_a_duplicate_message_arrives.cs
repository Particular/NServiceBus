﻿namespace NServiceBus.AcceptanceTests.Outbox;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_a_duplicate_message_arrives : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_invoke_handler_for_a_duplicate_message()
    {
        Requires.OutboxPersistence();

        var context = await Scenario.Define<Context>()
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
            .Done(c => c.Done && c.MessagesReceivedByDownstreamEndpoint >= 2 && c.MessagesReceivedByOutboxEndpoint >= 2)
            .Run();

        Assert.That(context.MessagesReceivedByDownstreamEndpoint, Is.EqualTo(2));
        Assert.That(context.MessagesReceivedByOutboxEndpoint, Is.EqualTo(2));
    }

    public class Context : ScenarioContext
    {
        public int MessagesReceivedByDownstreamEndpoint { get; set; }
        public bool Done { get; set; }
        public int MessagesReceivedByOutboxEndpoint { get; set; }
    }

    public class DownstreamEndpoint : EndpointConfigurationBuilder
    {
        public DownstreamEndpoint()
        {
            EndpointSetup<DefaultServer>(b => { b.LimitMessageProcessingConcurrencyTo(1); });
        }

        class SendOrderAcknowledgementHandler : IHandleMessages<SendOrderAcknowledgement>
        {
            public SendOrderAcknowledgementHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(SendOrderAcknowledgement message, IMessageHandlerContext context)
            {
                testContext.MessagesReceivedByDownstreamEndpoint++;
                if (message.Terminator)
                {
                    testContext.Done = true;
                }
                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public class OutboxEndpoint : EndpointConfigurationBuilder
    {
        public OutboxEndpoint()
        {
            EndpointSetup<DefaultServer>(b =>
            {
                // limit to one to avoid race conditions on dispatch and this allows us to reliably check whether deduplication happens properly
                b.LimitMessageProcessingConcurrencyTo(1);
                b.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                b.EnableOutbox();
                b.ConfigureRouting().RouteToEndpoint(typeof(SendOrderAcknowledgement), typeof(DownstreamEndpoint));
            });
        }

        class PlaceOrderHandler : IHandleMessages<PlaceOrder>
        {
            public PlaceOrderHandler(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(PlaceOrder message, IMessageHandlerContext context)
            {
                testContext.MessagesReceivedByOutboxEndpoint++;
                return context.Send(new SendOrderAcknowledgement
                {
                    Terminator = message.Terminator
                });
            }

            Context testContext;
        }
    }

    public class PlaceOrder : ICommand
    {
        public bool Terminator { get; set; }
    }

    public class SendOrderAcknowledgement : IMessage
    {
        public bool Terminator { get; set; }
    }
}