﻿namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    [NonParallelizable] // Ensure only activities for the current test are captured
    public class When_outgoing_activity_has_baggage : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_propagate_baggage_to_headers()
        {
            using var externalActivitySource = new ActivitySource("external trace source");
            using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();
            using var _ = TestingActivityListener.SetupDiagnosticListener(externalActivitySource.Name); // need to have a registered listener for activities to be created

            var context = await Scenario.Define<Context>()
                .WithEndpoint<TestEndpoint>(b =>
                    b.When(async session =>
                    {
                        // Otherwise the activity is created with a hierarchical ID format on .NET Framework which resets the RootId once it's converted to W3C format in the send pipeline.
                        var activityTraceContext = new ActivityContext(ActivityTraceId.CreateRandom(),
                            ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);

                        using (var wrapperActivity = externalActivitySource.StartActivity("ambient span", ActivityKind.Server, activityTraceContext))
                        {
                            wrapperActivity
                                ?.AddBaggage("key1", "value1")
                                ?.AddBaggage("key2", "value2");

                            await session.SendLocal(new SomeMessage());
                        }
                    })
                )
                .Done(ctx => ctx.MessageReceived)
                .Run();

            Assert.AreEqual("key2=value2,key1=value1", context.BaggageHeader);
        }

        class TestEndpoint : EndpointConfigurationBuilder
        {
            public TestEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class SomeMessageHandler : IHandleMessages<SomeMessage>
            {
                Context scenarioContext;

                public SomeMessageHandler(Context scenarioContext) => this.scenarioContext = scenarioContext;

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    if (context.MessageHeaders.TryGetValue(Headers.DiagnosticsBaggage, out var baggageValue))
                    {
                        scenarioContext.BaggageHeader = baggageValue;
                    }
                    scenarioContext.MessageReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        public class SomeMessage : IMessage
        {

        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; internal set; }
            public string BaggageHeader { get; internal set; }
        }
    }
}