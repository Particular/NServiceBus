namespace NServiceBus.AcceptanceTests.Core.Persistence;

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
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.SynchronizedStorageSessionInstanceInContainer, Is.Not.Null);
            Assert.That(result.SynchronizedStorageSessionInstanceInHandlingContext, Is.Not.Null);
        }
        Assert.That(result.SynchronizedStorageSessionInstanceInHandlingContext, Is.SameAs(result.SynchronizedStorageSessionInstanceInContainer));
    }

    public class Context : ScenarioContext
    {
        public ISynchronizedStorageSession SynchronizedStorageSessionInstanceInContainer { get; set; }
        public ISynchronizedStorageSession SynchronizedStorageSessionInstanceInHandlingContext { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public MyMessageHandler(Context testContext, ISynchronizedStorageSession storageSession)
            {
                this.testContext = testContext;
                testContext.SynchronizedStorageSessionInstanceInContainer = storageSession;
            }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.SynchronizedStorageSessionInstanceInHandlingContext = context.SynchronizedStorageSession;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            readonly Context testContext;
        }
    }

    public class MyMessage : IMessage;
}