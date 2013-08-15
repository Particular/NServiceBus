namespace NServiceBus.Persistence.Raven
{
    using System;
    using global::Raven.Client;

    public class StoreAccessor : IDisposable
    {
        private bool disposed;

        private readonly IDocumentStore store;

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
            if (disposed)
            {
                return;
            }

            if (store != null)
            {
                store.Dispose();
            }

            disposed = true;
        }
    }
}
