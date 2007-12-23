using System;
using System.Collections.Generic;
using NServiceBus.Unicast;
using NServiceBus.Multicast.Transport;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Multicast
{
    public class Bus : UnicastBus
    {
        #region config info

        private IList<string> subscribeToTopics = new List<string>();
        public IList<string> SubscribeToTopics
        {
            get { return subscribeToTopics; }
            set { subscribeToTopics = value; }
        }

        #endregion

        public override void Publish(params IMessage[] messages)
        {
            Msg m = this.GetMsgFor(messages);
            string address = this.GetDestinationForMessageType(messages[0].GetType());

            ((IMulticastTransport)this.transport).Publish(m, address);
        }

        public override void Subscribe(Type messageType)
        {
            this.Subscribe(messageType, null);
        }

        public override void Subscribe(Type messageType, Predicate<IMessage> condition)
        {
            this.subscriptionsManager.AddConditionForSubscriptionToMessageType(messageType, condition);

            string address = this.GetDestinationForMessageType(messageType);
            ((IMulticastTransport)this.transport).Subscribe(address);
        }

        public override void Unsubscribe(Type messageType)
        {
            string address = this.GetDestinationForMessageType(messageType);
            ((IMulticastTransport)this.transport).Unsubscribe(address);
        }

        public override void Start()
        {
            base.Start();

            foreach (string topic in this.subscribeToTopics)
                ((IMulticastTransport)this.transport).Subscribe(topic);
        }
    }
}
