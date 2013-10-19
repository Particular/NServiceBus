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
            Features.Sagas.ConfigureSaga(sagaType);
            FuncBuilder.Register(typeof(HeaderSagaIdFinder<>).MakeGenericType(sagaEntityType));
            RegisterMessageHandlerType<T>();

        }
    }
}