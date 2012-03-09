using System;
using System.Collections.Generic;

namespace Timeout.MessageHandlers
{
    public interface IPersistTimeouts
    {
        void Init();

        IEnumerable<TimeoutData> GetAll();

        void Add(TimeoutData timeout);

        bool Remove(TimeoutData timeout);

        void ClearAll(Guid sagaId);
    }
}
