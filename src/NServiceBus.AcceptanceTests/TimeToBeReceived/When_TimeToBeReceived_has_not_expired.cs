namespace NServiceBus.AcceptanceTests.TimeToBeReceived;

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
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.WasCalled, Is.True);
            Assert.That(context.TTBROnIncomingMessage, Is.EqualTo(TimeSpan.FromSeconds(10)), "TTBR should be available as a header so receiving endpoints can know what value was used when the message was originally sent");
        }
    }

    public class Context : ScenarioContext
    {
        public bool WasCalled { get; set; }
        public TimeSpan TTBROnIncomingMessage { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        public class MyMessageHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.TTBROnIncomingMessage = TimeSpan.Parse(context.MessageHeaders[Headers.TimeToBeReceived]);
                testContext.WasCalled = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    [TimeToBeReceived("00:00:10")]
    public class MyMessage : IMessage;
}