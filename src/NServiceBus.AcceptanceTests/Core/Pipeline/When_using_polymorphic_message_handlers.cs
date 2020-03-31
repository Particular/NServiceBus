namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
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
                .Done(c => c.SpecificHandlerInvoked && c.CatchAllHandlerInvoked)
                .Run();

            Assert.True(context.SpecificHandlerInvoked);
            Assert.True(context.CatchAllHandlerInvoked);
        }

        class Context : ScenarioContext
        {
            public bool CatchAllHandlerInvoked { get; set; }
            public bool SpecificHandlerInvoked { get; set; }
        }

        public class EndpointWithPolymorphicHandlers : EndpointConfigurationBuilder
        {
            public EndpointWithPolymorphicHandlers()
            {
                EndpointSetup<DefaultServer>();
            }

            class CatchAllHandler : IHandleMessages<ICommand>
            {
                public Context Context { get; set; }

                public CatchAllHandler(Context context)
                {
                    Context = context;
                }

                public Task Handle(ICommand message, IMessageHandlerContext context)
                {
                    Context.CatchAllHandlerInvoked = true;
                    return Task.FromResult(0);
                }
            }

            class SpecificHandler : IHandleMessages<SomeCommand>
            {
                public Context Context { get; set; }

                public SpecificHandler(Context context)
                {
                    Context = context;
                }

                public Task Handle(SomeCommand message, IMessageHandlerContext context)
                {
                    Context.SpecificHandlerInvoked = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class SomeCommand : ICommand
        {
        }
    }
}