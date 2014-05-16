namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using InMemory.SagaPersister;
    using NUnit.Framework;
    using Saga;
    using Sagas.Finders;

    public class with_sagas : using_the_unicastBus
    {
        protected InMemorySagaPersister persister;

        [SetUp]
        public new void SetUp()
        {
            persister = new InMemorySagaPersister();
            FuncBuilder.Register<ISagaPersister>(() => persister);

            Features.Sagas.Clear();
        }


        protected void RegisterCustomFinder<T>() where T : IFinder
        {
            Features.Sagas.ConfigureFinder(typeof(T));
        }
        protected void RegisterSaga<T>(object sagaEntity = null) where T : new()
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

            if (sagaEntity != null)
            {
                var se = (IContainSagaData) sagaEntity;

                persister.CurrentSagaEntities[se.Id] = new InMemorySagaPersister.VersionedSagaEntity { SagaEntity = se };
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