namespace NServiceBus.AcceptanceTests.TimeToBeReceived
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_TimeToBeReceived_has_expired : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Message_should_not_be_received()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>()
                .Run(TimeSpan.FromSeconds(10));

            Assert.IsFalse(context.WasCalled);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        class DelayReceiverFromStarting : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(b => new DelayReceiverFromStartingTask());
            }
        }

        class DelayReceiverFromStartingTask : FeatureStartupTask
        {
            protected override async Task OnStart(IMessageSession session)
            {
                await session.SendLocal(new MyMessage());
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            protected override Task OnStop(IMessageSession session)
            {
                return Task.FromResult(0);
            }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c => c.EnableFeature<DelayReceiverFromStarting>());
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public MyMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.WasCalled = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        [TimeToBeReceived("00:00:02")]
        public class MyMessage : IMessage
        {
        }
    }
}