using System;
using System.Collections.Generic;
using System.Text;
using NServiceBus.Unicast.Transport;
using NServiceBus.Unicast.Subscriptions;
using Common.Logging;

namespace NServiceBus.Unicast
{
    /// <summary>
    /// Thread safe
    /// </summary>
    public class SubscriptionsManager
    {
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

        public IList<Predicate<IMessage>> GetConditionsForMessage(IMessage message)
        {
            IList<Predicate<IMessage>> result = new List<Predicate<IMessage>>();

            lock (this.locker)
                if (this.messageTypeToConditionLookup.ContainsKey(message.GetType()))
                    foreach (Predicate<IMessage> condition in this.messageTypeToConditionLookup[message.GetType()])
                        result.Add(condition);

            return result;
        }

        public IList<string> GetSubscribersForMessage(IMessage message)
        {
            List<string> result = new List<string>();

            lock (this.locker)
                foreach (Entry e in this.entries)
                    if (e.MessageType == message.GetType())
                        result.Add(e.Msg.ReturnAddress);

            return result;
        }

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

        public bool HandledSubscriptionMessage(Msg msg)
        {
            return this.HandledSubscriptionMessage(msg, true);
        }

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


        private class Entry
        {
            public Entry(Type messageType, Msg msg)
            {
                this.msg = msg;
                this.messageType = messageType;
            }

            private Type messageType;
            public Type MessageType
            {
                get { return messageType; }
            }

            private Msg msg;
            public Msg Msg
            {
                get { return msg; }
            }
        }
    }
}
