using System;
using System.Collections.Generic;

namespace Timeout.MessageHandlers
{
    public interface IPersistTimeouts
    {
        IEnumerable<TimeoutData> GetAll();

        void Add(TimeoutData timeout);

        void Remove(Guid sagaId);
    }
}
