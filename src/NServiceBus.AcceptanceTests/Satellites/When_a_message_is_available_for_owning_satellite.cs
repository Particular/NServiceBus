namespace NServiceBus.AcceptanceTests.Satellites
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using Features;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Transport;

    public class When_a_message_is_available_for_owning_satellite : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) => session.SendLocal(new MyMessage())))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.True(context.MessageReceived);
            Assert.False(context.HandlerInvoked);
            // In the future we want the transport transaction to be an explicit
            // concept in the persisters API as well. Adding transport transaction
            // to the context will not be necessary at that point.
            // See GitHub issue #4047 for more background information.
            Assert.True(context.TransportTransactionAddedToContext);
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public bool MessageDispatched { get; set; }
            public bool TransportTransactionAddedToContext { get; set; }
            public bool HandlerInvoked { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(
                    c => c.LimitMessageProcessingConcurrencyTo(1));
            }

            public class MySatelliteFeature : Feature
            {
                public MySatelliteFeature()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.AddOwningSatelliteReceiver(
                        (c, ec) => RecoverabilityAction.MoveToError(c.Failed.ErrorQueue),
                        async (builder, dispatcher, messageContext) =>
                        {
                            var testContext = builder.Build<Context>();
                            
                            if (testContext.MessageDispatched && messageContext.Headers.TryGetValue("TestRunId", out var testRunId) && testRunId == testContext.TestRunId.ToString())
                            {
                                testContext.MessageReceived = true;
                            }
                            
                            testContext.TransportTransactionAddedToContext = ReferenceEquals(messageContext.Extensions.Get<TransportTransaction>(), messageContext.TransportTransaction);
                            var dictionary = new Dictionary<string, string>
                            {
                                ["TestRunId"] = testContext.TestRunId.ToString()
                            };
                            var transportOperation = new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), dictionary, new byte[0]), new UnicastAddressTag(context.Settings.LocalAddress()));
                            var transportOperations = new TransportOperations(transportOperation);
                            await dispatcher.Dispatch(transportOperations, messageContext.TransportTransaction, new ContextBag()).ConfigureAwait(false);
                            testContext.MessageDispatched = true;
                        });
                    
                }
            }
            
            class MyHandler : IHandleMessages<MyMessage>
            {
                Context testContext;

                public MyHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    testContext.HandlerInvoked = true;
                    return Task.FromResult(true);
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}