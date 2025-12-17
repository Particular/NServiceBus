namespace NServiceBus.AcceptanceTests.Core.Conventions;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_sending_with_conventions : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_receive_the_message()
    {
        var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
            .WithEndpoint<Endpoint>(b => b.When(async (session, c) =>
            {
                await session.SendLocal<MyMessage>(m => m.Id = c.Id);
                await session.SendLocal<IMyInterfaceMessage>(m => m.Id = c.Id);
            }))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.MessageClassReceived, Is.True);
            Assert.That(context.MessageInterfaceReceived, Is.True);
        }
    }

    public class Context : ScenarioContext
    {
        public bool MessageClassReceived { get; set; }
        public bool MessageInterfaceReceived { get; set; }
        public Guid Id { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(MessageClassReceived, MessageInterfaceReceived);
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>(b => b.Conventions().DefiningMessagesAs(type => type.Name.EndsWith("Message")));
    }

    public class MyMessage
    {
        public Guid Id { get; set; }
    }

    public interface IMyInterfaceMessage
    {
        Guid Id { get; set; }
    }

    public class MyMessageHandler(Context testContext) : IHandleMessages<MyMessage>
    {
        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            if (testContext.Id != message.Id)
            {
                return Task.CompletedTask;
            }

            testContext.MessageClassReceived = true;
            testContext.MaybeCompleted();

            return Task.CompletedTask;
        }
    }

    public class MyMessageInterfaceHandler(Context testContext) : IHandleMessages<IMyInterfaceMessage>
    {
        public Task Handle(IMyInterfaceMessage interfaceMessage, IMessageHandlerContext context)
        {
            if (testContext.Id != interfaceMessage.Id)
            {
                return Task.CompletedTask;
            }

            testContext.MessageInterfaceReceived = true;
            testContext.MaybeCompleted();

            return Task.CompletedTask;
        }
    }
}