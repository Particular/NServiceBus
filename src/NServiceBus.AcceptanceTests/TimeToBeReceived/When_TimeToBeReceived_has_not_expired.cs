namespace NServiceBus.AcceptanceTests.TimeToBeReceived
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_TimeToBeReceived_has_not_expired : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Message_should_be_received()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) => session.SendLocal(new MyMessage())))
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
                public MyMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    testContext.TTBROnIncomingMessage = TimeSpan.Parse(context.MessageHeaders[Headers.TimeToBeReceived]);
                    testContext.WasCalled = true;
                    return Task.FromResult(0);
                }

                Context testContext;

            }
        }

        [TimeToBeReceived("00:00:10")]
        public class MyMessage : IMessage
        {
        }
    }
}