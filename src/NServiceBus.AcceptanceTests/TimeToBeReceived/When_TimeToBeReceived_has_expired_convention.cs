namespace NServiceBus.AcceptanceTests.TimeToBeReceived
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_TimeToBeReceived_has_expired_convention : NServiceBusAcceptanceTest
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
                await Task.Delay(TimeSpan.FromMilliseconds(500));
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
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<DelayReceiverFromStarting>();
                    c.Conventions().DefiningTimeToBeReceivedAs(messageType =>
                    {
                        if (messageType == typeof(MyMessage))
                        {
                            return TimeSpan.FromMilliseconds(100);
                        }
                        return TimeSpan.MaxValue;
                    });
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    Context.WasCalled = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}