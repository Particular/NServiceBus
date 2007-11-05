using System;
using System.Collections.Generic;

namespace NServiceBus.Unicast.Transport
{
    public interface ITransport
    {
        void Start();

        IList<Type> MessageTypesToBeReceived { set; }

        void Send(Msg m, string destination);
        void ReceiveMessageLater(Msg m);

        string Address { get; }

        event EventHandler<MsgReceivedEventArgs> MsgReceived;
    }

    public class MsgReceivedEventArgs : EventArgs
    {
        public MsgReceivedEventArgs(Msg m)
        {
            this.message = m;
        }

        private Msg message;
        public Msg Message
        {
            get { return message; }
        }
    }
}
