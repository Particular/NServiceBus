namespace NServiceBus.AcceptanceTests.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_headers_contain_special_characters : NServiceBusAcceptanceTest
    {
        static Dictionary<string, string> sentHeaders = new Dictionary<string, string>
        {
            { "a-B1", "a-B" },
            { "a-B2", "a-ɤϡ֎ᾣ♥-b" },
            { "a-ɤϡ֎ᾣ♥-B3", "a-B" },
            { "a-B4", "a-\U0001F60D-b" },
            { "a-\U0001F605-B5", "a-B" },
            { "a-B6", "a-😍-b" },
            { "a-😅-B7", "a-B" },
            {"a-b8", "奥曼克"},
            {"a-B9", "٩(-̮̮̃-̃)۶ ٩(●̮̮̃•̃)۶ ٩(͡๏̯͡๏)۶ ٩(-̮̮̃•̃)" },
            {"a-b10", "தமிழ்" }
        };

        [Test]
        public async Task Outbox_should_work()
        {
            Requires.OutboxPersistence();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<OutboxEndpoint>(b => b.When(session => session.SendLocal(new PlaceOrder())))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.IsNotEmpty(context.UnicodeHeaders);
            CollectionAssert.IsSubsetOf(sentHeaders, context.UnicodeHeaders);
        }

        class Context : ScenarioContext
        {
            public IReadOnlyDictionary<string, string> UnicodeHeaders { get; set; }
            public bool MessageReceived { get; set; }
            public bool SavedOutBoxRecord { get; set; }
        }

        public class OutboxEndpoint : EndpointConfigurationBuilder
        {
            public OutboxEndpoint()
            {
                EndpointSetup<DefaultServer>((b, r) =>
                {
                    b.EnableOutbox();
                    b.Pipeline.Register("BlowUpBeforeDispatchBehavior", new BlowUpBeforeDispatchBehavior((Context)r.ScenarioContext), "Force reading the message from Outbox storage.");
                    b.Recoverability().Immediate(a => a.NumberOfRetries(1));
                });
            }
            class BlowUpBeforeDispatchBehavior : IBehavior<IBatchDispatchContext, IBatchDispatchContext>
            {
                Context testContext;

                public BlowUpBeforeDispatchBehavior(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Invoke(IBatchDispatchContext context, Func<IBatchDispatchContext, CancellationToken, Task> next, CancellationToken cancellationToken)
                {
                    if (!testContext.SavedOutBoxRecord)
                    {
                        testContext.SavedOutBoxRecord = true;
                        throw new Exception();
                    }

                    return next(context, cancellationToken);
                }
            }

            class PlaceOrderHandler : IHandleMessages<PlaceOrder>
            {
                public Task Handle(PlaceOrder message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    var sendOrderAcknowledgement = new SendOrderAcknowledgement();
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    foreach (var header in sentHeaders)
                    {
                        sendOptions.SetHeader(header.Key, header.Value);
                    }
                    return context.Send(sendOrderAcknowledgement, sendOptions);
                }
            }

            class SendOrderAcknowledgementHandler : IHandleMessages<SendOrderAcknowledgement>
            {
                public SendOrderAcknowledgementHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(SendOrderAcknowledgement message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.MessageReceived = true;
                    testContext.UnicodeHeaders = context.MessageHeaders.ToDictionary(x => x.Key, x => x.Value);
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class PlaceOrder : ICommand
        {
        }

        public class SendOrderAcknowledgement : IMessage
        {
        }
    }
}