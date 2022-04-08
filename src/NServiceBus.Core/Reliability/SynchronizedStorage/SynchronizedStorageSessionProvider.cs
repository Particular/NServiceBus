namespace NServiceBus.Persistence
{
    using System;
    using Janitor;

    /// <summary>
    /// Provides access to the <see cref="ISynchronizedStorageSession"/>.
    /// </summary>
    [SkipWeaving]
    public sealed class SynchronizedStorageSessionProvider : ISynchronizedStorageSessionProvider, IDisposable
    {
        /// <inheritdoc cref="ISynchronizedStorageSessionProvider.SynchronizedStorageSession"/>
        public ISynchronizedStorageSession SynchronizedStorageSession { get; set; }

        /// <inheritdoc />
        public void Dispose() => SynchronizedStorageSession = null;
    }
}
