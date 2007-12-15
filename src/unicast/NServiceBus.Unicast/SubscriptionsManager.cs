using System;
using System.Collections.Generic;
using System.Text;
using NServiceBus.Unicast.Transport;
using NServiceBus.Unicast.Subscriptions;
using Common.Logging;

namespace NServiceBus.Unicast
{
    /// <summary>
    /// Manages subscriptions for published messages.
    /// </summary>
	/// <remarks>
	/// Thread safe.
	/// </remarks>
    public class SubscriptionsManager
    {
		/// <summary>
		/// Sets the <see cref="ISubscriptionStorage"/> implementation to use for
		/// accessing stored subscriptions.
		/// </summary>
        public ISubscriptionStorage Storage
        {
            set
            {
                this.storage = value;

                if (this.storage != null)
                    foreach (Msg m in this.storage.GetAllMessages())
                        this.HandledSubscriptionMessage(m, false);
            }
        }

		/// <summary>
		/// Gets the list of conditions associated with a message.
		/// </summary>
		/// <param name="message">The message to get conditions for.</param>
		/// <returns>A list of conditions that are associated with type of message provided.</returns>
        public IList<Predicate<IMessage>> GetConditionsForMessage(IMessage message)
        {
            IList<Predicate<IMessage>> result = new List<Predicate<IMessage>>();

            lock (this.locker)
                if (this.messageTypeToConditionLookup.ContainsKey(message.GetType()))
                    foreach (Predicate<IMessage> condition in this.messageTypeToConditionLookup[message.GetType()])
                        result.Add(condition);

            return result;
        }

		/// <summary>
		/// Gets a list of the addresses of subscribers for the specified message.
		/// </summary>
		/// <param name="message">The message to get subscribers for.</param>
		/// <returns>A list of subscriber addresses.</returns>
        public IList<string> GetSubscribersForMessage(IMessage message)
        {
            List<string> result = new List<string>();

            lock (this.locker)
                foreach (Entry e in this.entries)
                    if (e.MessageType == message.GetType())
                        result.Add(e.Msg.ReturnAddress);

            return result;
        }

		/// <summary>
		/// Adds a condition to a message type.
		/// </summary>
		/// <param name="messageType">The message type to add a condition to.</param>
		/// <param name="condition">The condition to add.</param>
		/// <remarks>
		/// All conditions added to a message type must be met if the messages of that type 
		/// are to be published to a subscriber.</remarks>
        public void AddConditionForSubscriptionToMessageType(Type messageType, Predicate<IMessage> condition)
        {
            if (condition == null)
                return;

            lock (this.locker)
            {
                if (!this.messageTypeToConditionLookup.ContainsKey(messageType))
                    this.messageTypeToConditionLookup.Add(messageType, new List<Predicate<IMessage>>());

                if (!this.messageTypeToConditionLookup[messageType].Contains(condition))
                    this.messageTypeToConditionLookup[messageType].Add(condition);
            }
        }

		/// <summary>
		/// Attempts to handle a subscription message.
		/// </summary>
		/// <param name="msg">The message to attempt to handle.</param>
		/// <returns>true if the message was a valid subscription message, otherwise false.</returns>
        public bool HandledSubscriptionMessage(Msg msg)
        {
            return this.HandledSubscriptionMessage(msg, true);
        }

		/// <summary>
		/// Attempts to handle a subscription message allowing specification of whether or not
		/// the subscription persistence store should be updated.
		/// </summary>
		/// <param name="msg">The message to attempt to handle.</param>
		/// <param name="updateQueue">Whether or not the subscription persistence store should be updated.</param>
		/// <returns>true if the message was a valid subscription message, otherwise false.</returns>
        private bool HandledSubscriptionMessage(Msg msg, bool updateQueue)
        {
            IMessage[] messages = msg.Body;
            if (messages == null)
                return false;

            if (messages.Length != 1)
                return false;

            SubscriptionMessage subMessage = messages[0] as SubscriptionMessage;

            if (subMessage != null)
            {
                Type messageType = Type.GetType(subMessage.typeName, false);
                if (messageType == null)
                    log.Debug("Could not handle subscription for message type: " + subMessage.typeName);
                else
                {
                    if (this.storage == null)
                        throw new InvalidOperationException("Cannot process subscription messages without a SubscriptionsQueue.");

                    this.HandleAddSubscription(msg, messageType, subMessage, updateQueue);
                    this.HandleRemoveSubscription(msg, messageType, subMessage, updateQueue);
                }

                return true;
            }

            return false;
        }

		/// <summary>
		/// Handles a adding a subscription.
		/// </summary>
		/// <param name="msg">The message to handle.</param>
		/// <param name="messageType">The message type being subscribed to.</param>
		/// <param name="subMessage">A subscription message.</param>
		/// <param name="updateQueue">Whether or not to update the subscription persistence store.</param>
        private void HandleAddSubscription(Msg msg, Type messageType, SubscriptionMessage subMessage, bool updateQueue)
        {
            if (subMessage.subscriptionType == SubscriptionType.Add)
            {
                lock (this.locker)
                {
                    foreach (Entry e in this.entries)
                        if (e.MessageType == messageType && e.Msg.ReturnAddress == msg.ReturnAddress)
                            return;

                    if (updateQueue)
                        this.storage.Add(msg);

                    this.entries.Add(new Entry(messageType, msg));
                }
            }
        }

		/// <summary>
		/// Handles a removing a subscription.
		/// </summary>
		/// <param name="msg">The message to handle.</param>
		/// <param name="messageType">The message type being subscribed to.</param>
		/// <param name="subMessage">A subscription message.</param>
		/// <param name="updateQueue">Whether or not to update the subscription persistence store.</param>
        private void HandleRemoveSubscription(Msg msg, Type messageType, SubscriptionMessage subMessage, bool updateQueue)
        {
            if (subMessage.subscriptionType == SubscriptionType.Remove)
            {
                lock (this.locker)
                {
                    foreach (Entry e in this.entries.ToArray())
                        if (e.MessageType == messageType && e.Msg.ReturnAddress == msg.ReturnAddress)
                        {
                            if (updateQueue)
                                this.storage.Remove(e.Msg);

                            this.entries.Remove(e);
                        }
                }
            }
        }


        private ISubscriptionStorage storage;
        private List<Entry> entries = new List<Entry>();
        private IDictionary<Type, List<Predicate<IMessage>>> messageTypeToConditionLookup = new Dictionary<Type, List<Predicate<IMessage>>>();

        private object locker = new object();

        private static ILog log = LogManager.GetLogger(typeof(UnicastBus));

		/// <summary>
		/// Describes an entry in the list of subscriptions.
		/// </summary>
        private class Entry
        {
			/// <summary>
			/// Initializes a new Entry for the provided message type and
			/// subscription request.
			/// </summary>
			/// <param name="messageType"></param>
			/// <param name="msg"></param>
            public Entry(Type messageType, Msg msg)
            {
                this.msg = msg;
                this.messageType = messageType;
            }

            private Type messageType;
            
			/// <summary>
			/// Gets the message type for the subscription entry.
			/// </summary>
			public Type MessageType
            {
                get { return messageType; }
            }

            private Msg msg;

			/// <summary>
			/// Gets the subscription request message.
			/// </summary>
            public Msg Msg
            {
                get { return msg; }
            }
        }
    }
}
