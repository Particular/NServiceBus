namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;
    using Persistence.InMemory.SagaPersister;
    using Saga;
    using Sagas.Finders;

    public class with_sagas : using_the_unicastBus
    {
        protected InMemorySagaPersister persister;

        [SetUp]
        public void SetUp()
        {

            persister = new InMemorySagaPersister();
            FuncBuilder.Register<ISagaPersister>(() => persister);

            Features.Sagas.Clear();
        }

        protected void RegisterExistingSagaEntity(IContainSagaData sagaEntity)
        {
            persister.CurrentSagaEntities[sagaEntity.Id] = new InMemorySagaPersister.VersionedSagaEntity { SagaEntity = sagaEntity };

        }

        protected void RegisterSaga<T>() where T : new()
        {
            var sagaType = typeof(T);


            var args = sagaType.BaseType.GetGenericArguments();

            Type sagaEntityType = null;
            foreach (var type in args)
            {
                if (typeof(IContainSagaData).IsAssignableFrom(type))
                    sagaEntityType = type;

            }
            
            var sagaHeaderIdFinder = typeof(HeaderSagaIdFinder<>).MakeGenericType(sagaEntityType); 
            FuncBuilder.Register(sagaHeaderIdFinder);

            Features.Sagas.ConfigureSaga(sagaType);
            Features.Sagas.ConfigureFinder(sagaHeaderIdFinder);

            RegisterMessageHandlerType<T>();

        }
    }
}