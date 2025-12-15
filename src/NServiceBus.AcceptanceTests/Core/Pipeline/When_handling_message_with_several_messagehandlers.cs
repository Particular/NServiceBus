namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_handling_message_with_several_messagehandlers : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_call_all_handlers()
    {
        var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
            .WithEndpoint<Endpoint>(b => b.When((session, c) => session.SendLocal(new MyMessage
            {
                Id = c.Id
            })))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.FirstHandlerWasCalled, Is.True);
            Assert.That(context.SecondHandlerWasCalled, Is.True);
        }
    }

    public class Context : ScenarioContext
    {
        public bool FirstHandlerWasCalled { get; set; }
        public bool SecondHandlerWasCalled { get; set; }
        public Guid Id { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>(c => c.ConfigureRouting().RouteToEndpoint(typeof(MyMessage), typeof(Endpoint)));
    }

    public class MyMessage : IMessage
    {
        public Guid Id { get; set; }
    }

    public class FirstMessageHandler(Context testContext) : IHandleMessages<MyMessage>
    {
        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            if (testContext.Id != message.Id)
            {
                return Task.CompletedTask;
            }

            testContext.FirstHandlerWasCalled = true;
            return Task.CompletedTask;
        }
    }

    public class SecondMessageHandler(Context testContext) : IHandleMessages<MyMessage>
    {
        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            if (testContext.Id != message.Id)
            {
                return Task.CompletedTask;
            }

            testContext.SecondHandlerWasCalled = true;
            testContext.MarkAsCompleted();
            return Task.CompletedTask;
        }
    }
}