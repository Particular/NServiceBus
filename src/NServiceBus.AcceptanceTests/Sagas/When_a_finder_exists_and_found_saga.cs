namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using NServiceBus;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;

    [TestFixture]
    public class When_a_finder_exists_and_found_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_find_saga_and_not_correlate()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b.When(session => session.SendLocal(new StartSagaMessage { Property = "Test" })))
                .Done(c => c.Completed)
                .Run();

            Assert.True(context.FinderUsed);
        }

        public class Context : ScenarioContext
        {
            public bool FinderUsed { get; set; }
            public bool Completed { get; set; }
            public Guid SagaId { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class CustomFinder : IFindSagas<TestSaga08.SagaData08>.Using<SomeOtherMessage>
            {
                // ReSharper disable once MemberCanBePrivate.Global
                public Context Context { get; set; }

                public ISagaPersister SagaPersister { get; set; }
                
                public async Task<TestSaga08.SagaData08> FindBy(SomeOtherMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
                {
                    Context.FinderUsed = true;
                    return await SagaPersister.Get<TestSaga08.SagaData08>(Context.SagaId, storageSession, (ContextBag)context).ConfigureAwait(false);
                }
            }

            /// <summary>
            /// A helper handler saving saga and thend sending <see cref="SomeOtherMessage"/> to use finder on the preexisting saga.
            /// </summary>
            public class SavingSaga : IHandleMessages<StartSagaMessageByHandler>
            {
                public ISagaPersister SagaPersister { get; set; }
                public Context Context { get; set; }

                public async Task Handle(StartSagaMessageByHandler message, IMessageHandlerContext context)
                {
                    var sagaInstance = new TestSaga08.SagaData08
                    {
                        Property = Guid.NewGuid().ToString("N"),
                        Id = Guid.NewGuid()
                    };

                    Context.SagaId = sagaInstance.Id;
                    
                    await SagaPersister.Save(sagaInstance, SagaCorrelationProperty.None, context.SynchronizedStorageSession, context.Extensions).ConfigureAwait(false);
                    await context.SendLocal(new SomeOtherMessage());
                }
            }

            public class TestSaga08 : Saga<TestSaga08.SagaData08>,
                IAmStartedByMessages<StartSagaMessage>,
                IHandleMessages<SomeOtherMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new StartSagaMessageByHandler());
                }

                public Task Handle(SomeOtherMessage message, IMessageHandlerContext context)
                {
                    TestContext.Completed = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData08> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(saga => saga.Property).ToSaga(saga => saga.Property);
                    // Mapping not required for SomeOtherMessage because CustomFinder used
                }

                public class SagaData08 : ContainSagaData
                {
                    public virtual string Property { get; set; }
                }
            }
        }

        public class StartSagaMessage : IMessage
        {
            public string Property { get; set; }
        }

        public class StartSagaMessageByHandler : IMessage
        {
            public string Property { get; set; }
        }

        public class SomeOtherMessage : IMessage
        {
            public string Property { get; set; }
        }
    }
}
