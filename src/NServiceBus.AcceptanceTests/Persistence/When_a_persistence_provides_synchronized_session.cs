namespace NServiceBus.AcceptanceTests.Core.Persistence
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Persistence;
    using NUnit.Framework;

    public class When_a_persistence_provides_synchronized_session : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Synchronized_session_should_be_of_exact_type_provided_by_persistence()
        {
            var result = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(e => e.When(b => b.SendLocal(new MyMessage())))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.IsNotNull(result.SynchronizedStorageSessionInstanceInContainer);
            Assert.IsNotNull(result.SynchronizedStorageSessionInstanceInHandlingContext);
            Assert.AreSame(result.SynchronizedStorageSessionInstanceInContainer, result.SynchronizedStorageSessionInstanceInHandlingContext);
        }

        class Context : ScenarioContext
        {
            public ISynchronizedStorageSession SynchronizedStorageSessionInstanceInContainer { get; set; }
            public ISynchronizedStorageSession SynchronizedStorageSessionInstanceInHandlingContext { get; set; }
            public bool MessageReceived { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint() => EndpointSetup<DefaultServer>();

            class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public MyMessageHandler(Context testContext, ISynchronizedStorageSession storageSession)
                {
                    this.testContext = testContext;
                    testContext.SynchronizedStorageSessionInstanceInContainer = storageSession;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    testContext.SynchronizedStorageSessionInstanceInHandlingContext = context.SynchronizedStorageSession;
                    testContext.MessageReceived = true;
                    return Task.CompletedTask;
                }

                readonly Context testContext;
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}