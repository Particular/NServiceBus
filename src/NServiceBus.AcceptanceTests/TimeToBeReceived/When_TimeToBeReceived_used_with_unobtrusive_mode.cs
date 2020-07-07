namespace NServiceBus.AcceptanceTests.TimeToBeReceived
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_TimeToBeReceived_used_with_unobtrusive_mode : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Message_should_not_be_received()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>()
                .WithEndpoint<Receiver>()
                .Run(TimeSpan.FromSeconds(10));

            Assert.IsFalse(context.WasCalled);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        class SendMessageWhileStarting : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(b => new SendMessageWhileStartingTask());
            }
        }

        class SendMessageWhileStartingTask : FeatureStartupTask
        {
            protected override Task OnStart(IMessageSession session)
            {
                return session.Send(new MyCommand());
            }

            protected override Task OnStop(IMessageSession session)
            {
                return Task.FromResult(0);
            }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
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
                    c.EnableFeature<SendMessageWhileStarting>();
                    c.ConfigureTransport().Routing().RouteToEndpoint(typeof(MyCommand), typeof(Receiver));
                }).ExcludeType<MyCommand>(); // remove that type from assembly scanning to simulate what would happen with true unobtrusive mode
            }
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
            protected override Task OnStart(IMessageSession session)
            {
                return Task.Delay(TimeSpan.FromSeconds(5));
            }

            protected override Task OnStop(IMessageSession session)
            {
                return Task.FromResult(0);
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Conventions().DefiningCommandsAs(t => t.Namespace != null && t.FullName == typeof(MyCommand).FullName);
                    c.EnableFeature<DelayReceiverFromStarting>();
                });
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