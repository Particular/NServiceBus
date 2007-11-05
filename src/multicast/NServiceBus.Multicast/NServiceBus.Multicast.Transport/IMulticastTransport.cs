using System;
using System.Collections.Generic;
using System.Text;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Multicast.Transport
{
    public interface IMulticastTransport : ITransport
    {
        void Subscribe(string address);

        void Unsubscribe(string address);

        void Publish(Msg message, string address);
    }
}
