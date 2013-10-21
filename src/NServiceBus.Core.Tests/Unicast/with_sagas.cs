namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Linq;
    using Contexts;
    using Encryption;
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
            var sagaEntityType = GetSagaEntityType<T>();

            var sagaHeaderIdFinder = typeof(HeaderSagaIdFinder<>).MakeGenericType(sagaEntityType);
            FuncBuilder.Register(sagaHeaderIdFinder);

            Features.Sagas.ConfigureSaga(typeof(T));
            Features.Sagas.ConfigureFinder(sagaHeaderIdFinder);

            if (Features.Sagas.SagaEntityToMessageToPropertyLookup.ContainsKey(sagaEntityType))
            {
                foreach (var entityLookups in Features.Sagas.SagaEntityToMessageToPropertyLookup[sagaEntityType])
                {
                    var propertyFinder = typeof(PropertySagaFinder<,>).MakeGenericType(sagaEntityType, entityLookups.Key);

                    Features.Sagas.ConfigureFinder(propertyFinder);

                    var propertyLookups = entityLookups.Value;

                    var finder = Activator.CreateInstance(propertyFinder);
                    propertyFinder.GetProperty("SagaProperty").SetValue(finder, propertyLookups.Key);
                    propertyFinder.GetProperty("MessageProperty").SetValue(finder, propertyLookups.Value);
                    FuncBuilder.Register(propertyFinder, () => finder);
                }
            }
            RegisterMessageHandlerType<T>();

        }

        static Type GetSagaEntityType<T>() where T : new()
        {
            var sagaType = typeof(T);


            var args = sagaType.BaseType.GetGenericArguments();
            foreach (var type in args)
            {
                if (typeof(IContainSagaData).IsAssignableFrom(type))
                {
                    return type;
                }
            }
            return null;
        }
    }
}