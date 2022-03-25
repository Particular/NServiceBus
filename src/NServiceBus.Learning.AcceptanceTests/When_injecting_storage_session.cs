namespace NServiceBus.AcceptanceTests.Audit
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Persistence;

    public class When_injecting_storage_session : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_work()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithSeparateBodyStorage>(b => b.When((session, c) => session.SendLocal(new SentMessage())))
                .Done(c => c.ProviderWasInjected)
                .Run();

            Assert.True(context.ProviderWasInjected);
        }

        public class Context : ScenarioContext
        {
            public bool ProviderWasInjected { get; set; }
        }

        public class EndpointWithSeparateBodyStorage : EndpointConfigurationBuilder
        {
            public EndpointWithSeparateBodyStorage()
            {
                EndpointSetup<DefaultServer, Context>((config, context) =>
                 {
                     config.RegisterComponents(c => c.AddTransient<SomeComponentInjectedIntoTheHandler>());
                 });
            }

            public class SentMessageHandler : IHandleMessages<SentMessage>
            {
                readonly SomeComponentInjectedIntoTheHandler component;
                readonly ISynchronizedStorageSessionProvider sessionProvider;
                readonly Context testContext;

                public SentMessageHandler(SomeComponentInjectedIntoTheHandler component, ISynchronizedStorageSessionProvider sessionProvider, Context testContext)
                {
                    this.testContext = testContext;
                    this.sessionProvider = sessionProvider;
                    this.component = component;
                }

                public Task Handle(SentMessage sentMessage, IMessageHandlerContext context)
                {
                    testContext.ProviderWasInjected =
                        component.SessionProvider.SynchronizedStorageSession.Equals(sessionProvider
                            .SynchronizedStorageSession) &&
                        component.SessionProvider.SynchronizedStorageSession.Equals(context.SynchronizedStorageSession);
                    return Task.CompletedTask;
                }
            }

            public class SomeComponentInjectedIntoTheHandler
            {
                public ISynchronizedStorageSessionProvider SessionProvider { get; }

                public SomeComponentInjectedIntoTheHandler(ISynchronizedStorageSessionProvider sessionProvider)
                {
                    this.SessionProvider = sessionProvider;
                }
            }
        }

        public class SentMessage : IMessage
        {
        }
    }
}