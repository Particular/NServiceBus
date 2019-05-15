namespace NServiceBus.AcceptanceTests.DependencyInjection
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Particular.Approvals;

    public class When_enpoint_is_warmed_up : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Make_sure_things_are_in_DI()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<StartedEndpoint>(b=>b.When(e=>e.SendLocal(new SomeMessage())))
                .Done(c => c.GotTheMessage)
                .Run();

            Approver.Verify(string.Join(Environment.NewLine, spyContainer.RegisteredComponents.OrderBy(c=>c.Key.FullName)));
        }

        class Context : ScenarioContext
        {
            public bool GotTheMessage { get; set; }
        }

        public class StartedEndpoint : EndpointConfigurationBuilder
        {
            public StartedEndpoint()
            {
                EndpointSetup<DefaultServer>(c=>c.UseContainer(spyContainer));
            }

            class SomeMessageHandler:IHandleMessages<SomeMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    TestContext.GotTheMessage = true;

                    return Task.FromResult(0);
                }
            }
        }

        class SomeMessage : IMessage
        {
        }

        static AcceptanceTestingContainer spyContainer = new AcceptanceTestingContainer();
    }
}