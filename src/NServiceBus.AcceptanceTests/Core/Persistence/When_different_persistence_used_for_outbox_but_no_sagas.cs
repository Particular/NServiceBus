namespace NServiceBus.AcceptanceTests.Core.Persistence
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Persistence;
    using NUnit.Framework;

    public class When_different_persistence_used_for_outbox_but_no_sagas : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_throw() =>
            Assert.DoesNotThrowAsync(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(e => e.When(b => b.SendLocal(new MyMessage())))
                    .Done(c => c.MessageReceived)
                    .Run();
            });

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint() =>
                EndpointSetup<DefaultServer>(c =>
                {
                    c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;

                    c.EnableOutbox();
                    c.UsePersistence<FakePersistence, StorageType.Outbox>();
                });

            class MyHandler(Context testContext) : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageReceived = true;
                    return Task.CompletedTask;
                }
            }

            class FakePersistence : PersistenceDefinition, IPersistenceDefinitionFactory<FakePersistence>
            {
                FakePersistence()
                {
                    Supports<StorageType.Outbox, Outbox>();
                    Supports<StorageType.Sagas, Sagas>();
                }

                static FakePersistence IPersistenceDefinitionFactory<FakePersistence>.Create() => new();
            }
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }

        public class MyMessage : ICommand
        {
            public Guid DataId { get; set; }
        }
    }
}