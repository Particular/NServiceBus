
namespace NServiceBus.SagaPersisterTests
{
    using System;
    using NServiceBus.Saga;
    
    public static class TestSagaPersister
    {
        public static Func<Tuple<ISagaPersister, PersisterSession>> ConstructPersister = () =>
        {
            var persister = (ISagaPersister)Activator.CreateInstance(Type.GetType("NServiceBus.InMemory.SagaPersister.InMemorySagaPersister,NServiceBus.Core"));
            return Tuple.Create(persister, new PersisterSession());
        };

    }

// ReSharper disable once PartialTypeWithSinglePart
    public partial class PersisterSession
    {
        public void Begin()
        {
            OnBegin();
        }

        public void End()
        {
            OnEnd();
        }

// ReSharper disable once PartialMethodWithSinglePart
        partial void OnBegin();

// ReSharper disable once PartialMethodWithSinglePart
        partial void OnEnd();
    }
}
