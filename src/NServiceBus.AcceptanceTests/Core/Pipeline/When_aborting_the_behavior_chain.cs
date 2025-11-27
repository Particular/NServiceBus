namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_aborting_the_behavior_chain : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Subsequent_handlers_will_not_be_invoked()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<MyEndpoint>(b => b.When(session => session.SendLocal(new SomeMessage())))
            .Done(c => c.FirstHandlerInvoked)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.FirstHandlerInvoked, Is.True);
            Assert.That(context.SecondHandlerInvoked, Is.False);
        }
    }

    public class Context : ScenarioContext
    {
        public bool FirstHandlerInvoked { get; set; }
        public bool SecondHandlerInvoked { get; set; }
    }

    public class MyEndpoint : EndpointConfigurationBuilder
    {
        public MyEndpoint() => EndpointSetup<DefaultServer>(c =>
        {
            c.AddHandler<FirstHandler>();
            c.AddHandler<SecondHandler>();
        });

        public class FirstHandler(Context testContext) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.FirstHandlerInvoked = true;

                context.DoNotContinueDispatchingCurrentMessageToHandlers();

                return Task.CompletedTask;
            }
        }

        public class SecondHandler(Context testContext) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.SecondHandlerInvoked = true;

                return Task.CompletedTask;
            }
        }
    }

    public class SomeMessage : IMessage;
}