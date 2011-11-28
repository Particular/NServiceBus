namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;

    public interface IPersistTimeouts
    {
        IEnumerable<TimeoutData> GetAll();

        void Add(TimeoutData timeout);

        void Remove(Guid sagaId);
    }
}
