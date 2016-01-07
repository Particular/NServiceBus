namespace NServiceBus.AcceptanceTests.Performance.TimeToBeReceived
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
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
        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    TestContext.WasCalled = true;
                    return Task.FromResult(0);
                }
            }

            class DelayReceiverFromStarting: IWantToRunWhenBusStartsAndStops
            {
                /// <summary>
                /// Method called at startup.
                /// </summary>
                public async Task Start(IBusSession session)
                {
                    await session.SendLocal(new MyMessage());
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }

                /// <summary>
                /// Method called on shutdown.
                /// </summary>
                public Task Stop(IBusSession session)
                {
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        [TimeToBeReceived("00:00:02")]
        public class MyMessage : IMessage
        {
        }
    }
}
