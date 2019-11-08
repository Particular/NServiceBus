﻿namespace NServiceBus.AcceptanceTests.Core.DependencyInjection
{
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Particular.Approvals;

    public class When_endpoint_is_warmed_up : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Make_sure_things_are_in_DI()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<StartedEndpoint>(b => b.When(e => e.SendLocal(new SomeMessage())))
                .Done(c => c.GotTheMessage)
                .Run();

            var builder = new StringBuilder();
            var coreComponents = spyContainer.RegisteredComponents.Values
                .Where(c => c.Type.Assembly == typeof(IMessage).Assembly)
                    .OrderBy(c => c.Type.FullName)
                .ToList();

            builder.AppendLine("----------- Used registrations (Find ways to stop accessing them)-----------");

            foreach (var component in coreComponents.Where(c => c.WasResolved))
            {
                builder.AppendLine(component.ToString());
            }

            builder.AppendLine("----------- Registrations not used by the core, can be removed in next major if downstreams have been confirmed to not use it -----------");

            foreach (var component in coreComponents.Where(c => !c.WasResolved))
            {
                builder.AppendLine(component.ToString());
            }

            Approver.Verify(builder.ToString());
        }

        class Context : ScenarioContext
        {
            public bool GotTheMessage { get; set; }
        }

        public class StartedEndpoint : EndpointConfigurationBuilder
        {
            public StartedEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.UseContainer(spyContainer));
            }

            class SomeMessageHandler : IHandleMessages<SomeMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    TestContext.GotTheMessage = true;

                    return Task.FromResult(0);
                }
            }
        }

        public class SomeMessage : IMessage
        {
        }

        static AcceptanceTestingContainer spyContainer = new AcceptanceTestingContainer();
    }
}