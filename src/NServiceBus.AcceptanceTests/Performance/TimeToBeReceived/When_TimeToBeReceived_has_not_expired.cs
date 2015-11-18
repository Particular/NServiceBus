namespace NServiceBus.AcceptanceTests.Performance.TimeToBeReceived
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_TimeToBeReceived_has_not_expired : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Message_should_be_received()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.When((bus, c) => bus.SendLocal(new MyMessage())))
                    .Done(c => c.WasCalled)
                    .Run();

            Assert.IsTrue(context.WasCalled);
            Assert.AreEqual(TimeSpan.FromSeconds(10), context.TTBROnIncomingMessage, "TTBR should be available as a header so receiving endpoints can know what value was used when the message was originally sent");
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public TimeSpan TTBROnIncomingMessage { get; set; }
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
                    TestContext.TTBROnIncomingMessage = TimeSpan.Parse(context.MessageHeaders[Headers.TimeToBeReceived]);
                    TestContext.WasCalled = true;
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        [TimeToBeReceived("00:00:10")]
        public class MyMessage : IMessage
        {
        }
    }
}
