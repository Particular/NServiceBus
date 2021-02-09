namespace NServiceBus.AcceptanceTests.TimeToBeReceived
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_TimeToBeReceived_used_with_unobtrusive_mode : NServiceBusAcceptanceTest
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

        class SendMessageAndDelayStart : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(b => new SendMessageAndDelayStartTask());
            }
        }

        class SendMessageAndDelayStartTask : FeatureStartupTask
        {
            protected override async Task OnStart(IMessageSession session)
            {
                await session.SendLocal(new MyCommand());
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
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Conventions()
                    .DefiningCommandsAs(t => t.Namespace != null && t.FullName == typeof(MyCommand).FullName)
                    .DefiningTimeToBeReceivedAs(messageType =>
                    {
                        if (messageType == typeof(MyCommand))
                        {
                            return TimeSpan.FromSeconds(2);
                        }
                        return TimeSpan.MaxValue;
                    });
                    c.EnableFeature<SendMessageAndDelayStart>();
                }).ExcludeType<MyCommand>(); // remove that type from assembly scanning to simulate what would happen with true unobtrusive mode
            }

            public class MyMessageHandler : IHandleMessages<MyCommand>
            {
                public MyMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyCommand message, IMessageHandlerContext context)
                {
                    testContext.WasCalled = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MyCommand
        {
        }
    }
}