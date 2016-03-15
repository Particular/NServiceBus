using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Persistence;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NServiceBus.AcceptanceTests.Persistence
{
    public class When_a_persistence_does_not_provide_ISynchronizationContext
    {
        public class InMemoryNoSyncContextPersistence : PersistenceDefinition
        {
            internal InMemoryNoSyncContextPersistence()
            {
                Supports<StorageType.Timeouts>(s => { });
                Supports<StorageType.Sagas>(s => { });
                Supports<StorageType.Subscriptions>(s => { });
            }
        }

        [Test]
        public async Task ReceiveFeature_should_work_without_ISynchronizedStorage()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<NoSyncEndpoint>(e => e.When(b => b.SendLocal(new MyMessage())))
                .Done(c => c.Done)
                .Run();
        }

        public class NoSyncEndpoint : EndpointConfigurationBuilder
        {
            public NoSyncEndpoint()
            {
                EndpointSetup<PersistencelessServer>(c =>
                {
                    c.UsePersistence<InMemoryNoSyncContextPersistence>();
                });
            }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                Context.Done = true;

                return Task.FromResult(0);
            }
        }

        public class Context : ScenarioContext
        {
            public bool NotSet { get; set; }
            public bool Done { get; set; }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
        }
    }
}