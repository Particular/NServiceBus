using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel.Channels;
using System.ServiceModel;

namespace NServiceBus.Unicast.Transport.WCF
{
    [ServiceBehavior(InstanceContextMode=InstanceContextMode.Single)]
    public class OneWayService : IOneWay
    {
        #region IOneWay Members

        public void Process(Message m)
        {
            if (MessageReceived != null)
                MessageReceived(this, new MessageEventArgs(m));
        }

        #endregion

        public static event EventHandler<MessageEventArgs> MessageReceived;
    }

    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(Message m)
        {
            this.message = m;
        }

        private Message message;
        public Message Message
        {
            get { return message; }
        }
    }
}
