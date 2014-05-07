namespace NServiceBus.Core.Tests.Persistence.RavenDB.SubscriptionStorage
{
    using NServiceBus.Persistence.Raven;
    using NServiceBus.Persistence.Raven.SubscriptionStorage;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Embedded;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    // TODO Move away
//    public class WithRavenSubscriptionStorage
//    {
//        protected ISubscriptionStorage storage;
//        protected IDocumentStore store;
//
//        [SetUp]
//        public void SetupContext()
//        {
//            store = new EmbeddableDocumentStore { RunInMemory = true};
//            store.Conventions.DefaultQueryingConsistency = ConsistencyOptions.QueryYourWrites;
//           
//            store.Initialize();
//
//            storage = new RavenSubscriptionStorage(new StoreAccessor(store));
//            storage.Init();
//        }
//
//        [TearDown]
//        public void Cleanup()
//        {
//            if (store != null)
//            {
//                store.Dispose();
//            }
//        }
//    }
}