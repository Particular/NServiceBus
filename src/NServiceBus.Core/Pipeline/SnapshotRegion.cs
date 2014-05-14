namespace NServiceBus.Pipeline
{
    using System;
    using Janitor;

    [SkipWeaving]
    class SnapshotRegion : IDisposable
    {
        public SnapshotRegion(dynamic chain)
        {
            this.chain = chain;
            chain.TakeSnapshot();
        }

        public void Dispose()
        {
            chain.DeleteSnapshot();
        }

        readonly dynamic chain;
    }
}