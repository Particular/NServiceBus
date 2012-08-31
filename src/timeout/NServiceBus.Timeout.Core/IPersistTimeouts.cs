namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;

    public interface IPersistTimeouts
    {
        List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery);

        void Add(TimeoutData timeout);

        bool TryRemove(string timeoutId, out TimeoutData timeoutData);

        void RemoveTimeoutBy(Guid sagaId);
    }
}
