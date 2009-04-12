using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus.Proxy
{
    public interface ISubscriberStorage
    {
        void Store(string subscriber);

        void Remove(string subscriber);

        IEnumerable<string> GetAllSubscribers();
    }
}
