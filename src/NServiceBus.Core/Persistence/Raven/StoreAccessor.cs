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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed resources.
                store.Dispose();
            }

            disposed = true;
        }

        ~StoreAccessor()
        {
            Dispose(false);
        }
    }
}
