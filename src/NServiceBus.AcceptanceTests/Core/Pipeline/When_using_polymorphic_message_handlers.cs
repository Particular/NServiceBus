namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_using_polymorphic_message_handlers : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_invoke_all_compatible_handlers()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithPolymorphicHandlers>(b =>
            {
                b.When(session => session.SendLocal(new SomeCommand()));
            })
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.SpecificHandlerInvoked, Is.True);
            Assert.That(context.CatchAllHandlerInvoked, Is.True);
        }
    }

    class Context : ScenarioContext
    {
        public bool CatchAllHandlerInvoked { get; set; }
        public bool SpecificHandlerInvoked { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(SpecificHandlerInvoked, CatchAllHandlerInvoked);
    }

    public class EndpointWithPolymorphicHandlers : EndpointConfigurationBuilder
    {
        public EndpointWithPolymorphicHandlers() => EndpointSetup<DefaultServer>();

        class CatchAllHandler(Context testContext) : IHandleMessages<ICommand>
        {
            public Task Handle(ICommand message, IMessageHandlerContext context)
            {
                testContext.CatchAllHandlerInvoked = true;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }

        class SpecificHandler(Context testContext) : IHandleMessages<SomeCommand>
        {
            public Task Handle(SomeCommand message, IMessageHandlerContext context)
            {
                testContext.SpecificHandlerInvoked = true;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class SomeCommand : ICommand;
}