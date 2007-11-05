using System;
using System.Collections.Generic;
using System.Text;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast.Subscriptions
{
    public interface ISubscriptionStorage
    {
        IList<Msg> GetAllMessages();

        void Add(Msg m);
        void Remove(Msg m);
    }
}
