namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;

    public interface IPersistTimeouts
    {
        List<TimeoutData> GetNextChunk(out DateTime nextTimeToRunQuery);

        void Add(TimeoutData timeout);

        void Remove(string timeoutId);

        void RemoveTimeoutBy(Guid sagaId);
    }
}
