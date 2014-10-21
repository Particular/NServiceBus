namespace NServiceBus.SagaPersisterTests
{
    using System;
    using NServiceBus.Saga;
    using NUnit.Framework;

// ReSharper disable once PartialTypeWithSinglePart
    public partial class SagaPersisterTest
    {
        internal PersisterSession session;
        internal ISagaPersister persister;

// ReSharper disable once PartialMethodWithSinglePart
        partial void OnConstructPersister();

        [SetUp]
        public void CreatePersister()
        {
            persister = (ISagaPersister)Activator.CreateInstance(Type.GetType("NServiceBus.InMemory.SagaPersister.InMemorySagaPersister, NServiceBus.Core",true));
            session = new PersisterSession();
            OnConstructPersister();
        }
    }
}