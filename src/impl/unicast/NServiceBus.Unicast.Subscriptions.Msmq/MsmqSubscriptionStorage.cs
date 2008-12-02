#region License

/*
 * Copyright © 2007-2008 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;
using System.Collections.Generic;
using NServiceBus.Unicast.Transport;
using System.Messaging;
using NServiceBus.Unicast.Transport.Msmq;
using Common.Logging;

namespace NServiceBus.Unicast.Subscriptions.Msmq
{
	/// <summary>
	/// Provides functionality for managing message subscriptions
	/// using MSMQ.
	/// </summary>
    public class MsmqSubscriptionStorage : ISubscriptionStorage
    {
        #region ISubscriptionStorage Members

        public void Init(IList<Type> messageTypes)
        {
            foreach (Message m in this.q.GetAllMessages())
            {
                string subscriber = m.Label;
                string messageType = m.Body as string;

                this.entries.Add(new Entry(messageType, subscriber));
                this.AddToLookup(subscriber, messageType, m.Id);
            }
        }

        /// <summary>
        /// Gets a list of the addresses of subscribers for the specified message.
        /// </summary>
        /// <param name="messageType">The message type to get subscribers for.</param>
        /// <returns>A list of subscriber addresses.</returns>
        public IList<string> GetSubscribersForMessage(Type messageType)
        {
            List<string> result = new List<string>();

            lock (this.locker)
                foreach (Entry e in this.entries)
                    if (e.Matches(messageType))
                        result.Add(e.Subscriber);

            return result;
        }

        /// <summary>
        /// Attempts to handle a subscription message.
        /// </summary>
        /// <param name="msg">The message to attempt to handle.</param>
        /// <returns>true if the message was a valid subscription message, otherwise false.</returns>
        public void HandleSubscriptionMessage(TransportMessage msg)
        {
            this.HandleSubscriptionMessage(msg, true);
        }

        /// <summary>
        /// Attempts to handle a subscription message allowing specification of whether or not
        /// the subscription persistence store should be updated.
        /// </summary>
        /// <param name="msg">The message to attempt to handle.</param>
        /// <param name="updateQueue">Whether or not the subscription persistence store should be updated.</param>
        /// <returns>true if the message was a valid subscription message, otherwise false.</returns>
        private void HandleSubscriptionMessage(TransportMessage msg, bool updateQueue)
        {
            IMessage[] messages = msg.Body;
            if (messages == null)
                return;

            if (messages.Length != 1)
                return;

            SubscriptionMessage subMessage = messages[0] as SubscriptionMessage;

            if (subMessage != null)
            {
                if (subMessage.TypeName == null)
                {
                    log.Debug("Blank subscription message received.");
                    return;
                }

                Type messageType = Type.GetType(subMessage.TypeName, false);
                if (messageType == null)
                    log.Debug("Could not handle subscription for message type: " + subMessage.TypeName + ". Type not available on this endpoint.");
                else
                {
                    this.HandleAddSubscription(msg, messageType, subMessage, updateQueue);
                    this.HandleRemoveSubscription(msg, messageType, subMessage, updateQueue);
                }
            }
        }

        /// <summary>
        /// Checks the subscription type, and if it is 'Add', then adds the subscriber.
        /// </summary>
        /// <param name="msg">The message to handle.</param>
        /// <param name="messageType">The message type being subscribed to.</param>
        /// <param name="subMessage">A subscription message.</param>
        /// <param name="updateQueue">Whether or not to update the subscription persistence store.</param>
        private void HandleAddSubscription(TransportMessage msg, Type messageType, SubscriptionMessage subMessage, bool updateQueue)
        {
            if (subMessage.SubscriptionType == SubscriptionType.Add)
            {
                lock (this.locker)
                {
                    // if already subscribed, do nothing
                    foreach (Entry e in this.entries)
                        if (e.Matches(messageType) && e.Subscriber == msg.ReturnAddress)
                            return;

                    if (updateQueue)
                        this.Add(msg.ReturnAddress, subMessage.TypeName);

                    this.entries.Add(new Entry(messageType, msg));

                    log.Debug("Subscriber " + msg.ReturnAddress + " added for message " + messageType.FullName + ".");
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
        private void HandleRemoveSubscription(TransportMessage msg, Type messageType, SubscriptionMessage subMessage, bool updateQueue)
        {
            if (subMessage.SubscriptionType == SubscriptionType.Remove)
            {
                lock (this.locker)
                {
                    foreach (Entry e in this.entries.ToArray())
                        if (e.Matches(messageType) && e.Subscriber == msg.ReturnAddress)
                        {
                            if (updateQueue)
                                this.Remove(e.Subscriber, e.MessageType);

                            this.entries.Remove(e);

                            log.Debug("Subscriber " + msg.ReturnAddress + " removed for message " + messageType.FullName + ".");
                        }
                }
            }
        }

		/// <summary>
		/// Adds a message to the subscription store.
		/// </summary>
        public void Add(string subscriber, string typeName)
        {
		    Message toSend = new Message();
		    toSend.Formatter = q.Formatter;
            toSend.Recoverable = true;

		    toSend.Label = subscriber;
		    toSend.Body = typeName;

            this.q.Send(toSend, GetTransactionType());

            this.AddToLookup(subscriber, typeName, toSend.Id);
        }

		/// <summary>
		/// Removes a message from the subscription store.
		/// </summary>
        public void Remove(string subscriber, string typeName)
        {
            string messageId;

            lock (this.lookup)
            {
                if (!this.lookup.ContainsKey(subscriber))
                    return;

                this.lookup[subscriber].TryGetValue(typeName, out messageId);

                if (messageId == null)
                    return;
            }

		    this.q.ReceiveById(messageId, GetTransactionType());
        }

	    private MessageQueueTransactionType GetTransactionType()
	    {
	        MessageQueueTransactionType t = MessageQueueTransactionType.Automatic;
	        if (this.dontUseExternalTransaction)
	            t = MessageQueueTransactionType.Single;
	        return t;
	    }

	    #endregion

        #region config info

        private bool dontUseExternalTransaction;

		/// <summary>
		/// Gets/sets whether or not to use a trasaction started outside the 
		/// subscription store.
		/// </summary>
        public virtual bool DontUseExternalTransaction
        {
            get { return dontUseExternalTransaction; }
            set { dontUseExternalTransaction = value; }
        }

		/// <summary>
		/// Sets the address of the queue where subscription messages will be stored.
		/// For a local queue, just use its name - msmq specific info isn't needed.
		/// For a remote queue (supported MSMQ 4.0), use the format "queue@machine".
		/// </summary>
        public virtual string Queue
        {
            set
            {
                string path = MsmqTransport.GetFullPath(value);
                q = new MessageQueue(path);

                if (!q.Transactional)
                    throw new ArgumentException("Queue must be transactional (" + value + ").");

                MessagePropertyFilter mpf = new MessagePropertyFilter();
                mpf.SetAll();

                this.q.Formatter = new XmlMessageFormatter(new Type[] { typeof(string) });

                this.q.MessageReadPropertyFilter = mpf;
            }
        }

        #endregion

        #region helper methods

		/// <summary>
		/// Adds a message to the lookup to find message from
		/// subscriber, to message type, to message id
		/// </summary>
        private void AddToLookup(string subscriber, string typeName, string messageId)
        {
            lock (this.lookup)
            {
                if (!this.lookup.ContainsKey(subscriber))
                    this.lookup.Add(subscriber, new Dictionary<string, string>());

                if (!this.lookup[subscriber].ContainsKey(typeName))
                    this.lookup[subscriber].Add(typeName, messageId);
            }
        }

        #endregion

        #region members

        private MessageQueue q;

        /// <summary>
        /// lookup from subscriber, to message type, to message id
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> lookup = new Dictionary<string, Dictionary<string, string>>();

        private List<Entry> entries = new List<Entry>();
        private object locker = new object();

	    private ILog log = LogManager.GetLogger(typeof (ISubscriptionStorage));

        #endregion
    }
}
