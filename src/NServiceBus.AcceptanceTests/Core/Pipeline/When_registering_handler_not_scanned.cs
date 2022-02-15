namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_registering_handler_not_scanned : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_invoke_handler()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithoutAssemblyScanning>(e => e
                    .When(s => s.SendLocal(new IncomingMessage())))
                .Done(c => c.HandlerInvoked)
                .Run(TimeSpan.FromSeconds(15));
        }

        class Context : ScenarioContext
        {
            public bool HandlerInvoked { get; set; }
        }

        class EndpointWithoutAssemblyScanning : EndpointConfigurationBuilder
        {
            public EndpointWithoutAssemblyScanning() =>
                EndpointSetup<DefaultServer>(c =>
                {
                    c.AssemblyScanner().ScanAppDomainAssemblies = false;
                    c.AssemblyScanner().ScanFileSystemAssemblies = false;
                    c.RegisterMessageHandlers(typeof(RegisteredHandler));

                }).ExcludeType<RegisteredHandler>().ExcludeType<IncomingMessage>();

            public class RegisteredHandler : IHandleMessages<IncomingMessage>
            {
                Context testContext;

                public RegisteredHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(IncomingMessage message, IMessageHandlerContext context)
                {
                    testContext.HandlerInvoked = true;
                    return Task.CompletedTask;
                }
            }
        }

        public class IncomingMessage : IMessage
        {
        }
    }
}