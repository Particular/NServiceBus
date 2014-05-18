namespace NServiceBus.Persistence.Raven
{
    using System;
    using global::Raven.Client;

    public class StoreAccessor : IDisposable
    {
        IDocumentStore store;

        public StoreAccessor(IDocumentStore store)
        {
            this.store = store;
        }

        public IDocumentStore Store
        {
            get { return store; }
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        void DisposeManaged()
        {
            if (store != null)
            {
                store.Dispose();
            }
        }
    }
}
