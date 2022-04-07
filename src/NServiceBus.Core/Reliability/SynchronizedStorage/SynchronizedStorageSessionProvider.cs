namespace NServiceBus.Persistence
{
    using System;
    using Janitor;

    /// <summary>
    ///
    /// </summary>
    [SkipWeaving]
    public sealed class SynchronizedStorageSessionProvider : ISynchronizedStorageSessionProvider, IDisposable
    {
        /// <summary>
        ///
        /// </summary>
        public ISynchronizedStorageSession SynchronizedStorageSession { get; set; }

        /// <inheritdoc />
        public void Dispose()
        {
            SynchronizedStorageSession = null;
        }
    }
}
