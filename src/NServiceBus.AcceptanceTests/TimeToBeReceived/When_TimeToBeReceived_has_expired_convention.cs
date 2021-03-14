﻿namespace NServiceBus.AcceptanceTests.TimeToBeReceived
{
    using System;
    using System.Threading;
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
            protected override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default)
            {
                context.RegisterStartupTask(b => new DelayReceiverFromStartingTask());
                return Task.CompletedTask;
            }
        }

        class DelayReceiverFromStartingTask : FeatureStartupTask
        {
            protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {
                await session.SendLocal(new MyMessage(), cancellationToken: cancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
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
                            return TimeSpan.FromSeconds(2);
                        }
                        return TimeSpan.MaxValue;
                    });
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public MyMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    testContext.WasCalled = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}