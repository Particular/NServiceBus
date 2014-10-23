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
            OnConstructPersister();
            if (persister == null)
            {
                persister = (ISagaPersister)Activator.CreateInstance(Type.GetType("NServiceBus.InMemory.SagaPersister.InMemorySagaPersister, NServiceBus.Core", true));                
            }
            if (session == null)
            {
                session = (PersisterSession)Activator.CreateInstance(typeof(PersisterSession));
            }
        }
    }
}