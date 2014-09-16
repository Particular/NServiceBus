namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using InMemory.SagaPersister;
    using NServiceBus.Features;
    using Sagas;
    using NUnit.Framework;
    using Saga;
    using Sagas.Finders;

    class with_sagas : using_the_unicastBus
    {
        protected InMemorySagaPersister persister;
        Conventions conventions;
        Sagas sagas;

        [SetUp]
        public new void SetUp()
        {
            persister = new InMemorySagaPersister();
            FuncBuilder.Register<ISagaPersister>(() => persister);

            sagas = new Sagas();

            FuncBuilder.Register<SagaConfigurationCache>(() => sagas.sagaConfigurationCache);

            conventions = new Conventions();
        }

        protected override void ApplyPipelineModifications()
        {
            pipelineModifications.Additions.Add(new SagaPersistenceBehavior.SagaPersistenceRegistration());
        }

        protected void RegisterCustomFinder<T>() where T : IFinder
        {
            sagas.ConfigureFinder(typeof(T), conventions);
        }

        protected void RegisterSaga<T>(object sagaEntity = null) where T : new()
        {
            var sagaEntityType = GetSagaEntityType<T>();

            var sagaHeaderIdFinder = typeof(HeaderSagaIdFinder<>).MakeGenericType(sagaEntityType);
            FuncBuilder.Register(sagaHeaderIdFinder);

            sagas.ConfigureSaga(typeof(T), conventions);
            sagas.ConfigureFinder(sagaHeaderIdFinder, conventions);

            if (sagas.sagaConfigurationCache.SagaEntityToMessageToPropertyLookup.ContainsKey(sagaEntityType))
            {
                foreach (var entityLookups in sagas.sagaConfigurationCache.SagaEntityToMessageToPropertyLookup[sagaEntityType])
                {
                    var propertyFinder = typeof(PropertySagaFinder<,>).MakeGenericType(sagaEntityType, entityLookups.Key);

                    sagas.ConfigureFinder(propertyFinder, conventions);

                    var propertyLookups = entityLookups.Value;

                    var finder = Activator.CreateInstance(propertyFinder);
                    propertyFinder.GetProperty("SagaToMessageMap").SetValue(finder, propertyLookups);
                    FuncBuilder.Register(propertyFinder, () => finder);
                }
            }

            if (sagaEntity != null)
            {
                var se = (IContainSagaData)sagaEntity;

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