namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_receiving_with_catch_all_handlers_registered : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_call_catch_all_handlers()
    {
        var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
            .WithEndpoint<Endpoint>(b => b.When((session, c) => session.SendLocal(new MyMessage
            {
                Id = c.Id
            })))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ObjectHandlerWasCalled, Is.True);
            Assert.That(context.DynamicHandlerWasCalled, Is.True);
            Assert.That(context.IMessageHandlerWasCalled, Is.True);
        }
    }

    public class Context : ScenarioContext
    {
        public bool ObjectHandlerWasCalled { get; set; }
        public bool DynamicHandlerWasCalled { get; set; }
        public bool IMessageHandlerWasCalled { get; set; }
        public Guid Id { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(ObjectHandlerWasCalled, DynamicHandlerWasCalled, IMessageHandlerWasCalled);
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();
    }

    public class MyMessage : IMessage
    {
        public Guid Id { get; set; }
    }

    [Handler]
    public class CatchAllHandler_object(Context testContext) : IHandleMessages<object>
    {
        public Task Handle(object message, IMessageHandlerContext context)
        {
            var myMessage = (MyMessage)message;
            if (testContext.Id != myMessage.Id)
            {
                return Task.CompletedTask;
            }

            testContext.ObjectHandlerWasCalled = true;
            testContext.MaybeCompleted();
            return Task.CompletedTask;
        }
    }

    [Handler]
    public class CatchAllHandler_dynamic(Context testContext) : IHandleMessages<object>
    {
        public Task Handle(dynamic message, IMessageHandlerContext context)
        {
            var myMessage = (MyMessage)message;
            if (testContext.Id != myMessage.Id)
            {
                return Task.CompletedTask;
            }

            testContext.DynamicHandlerWasCalled = true;
            testContext.MaybeCompleted();
            return Task.CompletedTask;
        }
    }

    [Handler]
    public class CatchAllHandler_IMessage(Context testContext) : IHandleMessages<IMessage>
    {
        public Task Handle(IMessage message, IMessageHandlerContext context)
        {
            var myMessage = (MyMessage)message;
            if (testContext.Id != myMessage.Id)
            {
                return Task.CompletedTask;
            }

            testContext.IMessageHandlerWasCalled = true;
            testContext.MaybeCompleted();
            return Task.CompletedTask;
        }
    }
}