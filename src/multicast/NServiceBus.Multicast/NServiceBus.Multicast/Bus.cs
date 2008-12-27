using System;
using System.Collections.Generic;
using NServiceBus.Unicast;
using NServiceBus.Multicast.Transport;
using NServiceBus.Unicast.Transport;
using ObjectBuilder;

namespace NServiceBus.Multicast
{
    /// <summary>
    /// Demo-ware multicast implementation of the bus.
    /// </summary>
    public class Bus : UnicastBus
    {
        #region config info

        private IList<string> subscribeToTopics = new List<string>();

        /// <summary>
        /// Gets/sets topics to subscribe to.
        /// </summary>
        public IList<string> SubscribeToTopics
        {
            get { return subscribeToTopics; }
            set { subscribeToTopics = value; }
        }

        #endregion

        /// <summary>
        /// Publishes messages of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messages"></param>
        public override void Publish<T>(params T[] messages)
        {
            TransportMessage m = this.GetTransportMessageFor(string.Empty, messages as IMessage[]);
            m.ReturnAddress = this.transport.Address;

            string address = this.GetDestinationForMessageType(messages[0].GetType());

            ((IMulticastTransport)this.transport).Publish(m, address);
        }

        /// <summary>
        /// Subscribes to the given message type.
        /// </summary>
        /// <param name="messageType"></param>
        public override void Subscribe(Type messageType)
        {
            this.Subscribe(messageType, null);
        }

        /// <summary>
        /// Subscribes to the given message type, storing the given condition
        /// as to which messages should be processed.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="condition"></param>
        public override void Subscribe(Type messageType, Predicate<IMessage> condition)
        {
            this.subscriptionsManager.AddConditionForSubscriptionToMessageType(messageType, condition);

            string address = this.GetDestinationForMessageType(messageType);
            ((IMulticastTransport)this.transport).Subscribe(address);
        }

        /// <summary>
        /// Unsubscribes from the given message type.
        /// </summary>
        /// <param name="messageType"></param>
        public override void Unsubscribe(Type messageType)
        {
            string address = this.GetDestinationForMessageType(messageType);
            ((IMulticastTransport)this.transport).Unsubscribe(address);
        }

        /// <summary>
        /// Starts the bus performing the given startup actions.
        /// </summary>
        /// <param name="startupActions"></param>
        /// <returns></returns>
        public override IBus Start(params Action<IBuilder>[] startupActions)
        {
            base.Start(startupActions);

            foreach (string topic in this.subscribeToTopics)
                ((IMulticastTransport)this.transport).Subscribe(topic);

            return this;
        }
    }
}
