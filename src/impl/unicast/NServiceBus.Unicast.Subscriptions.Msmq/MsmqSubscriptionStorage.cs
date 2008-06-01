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

namespace NServiceBus.Unicast.Subscriptions.Msmq
{
	/// <summary>
	/// Provides functionality for managing message subscriptions
	/// using MSMQ.
	/// </summary>
    public class MsmqSubscriptionStorage : ISubscriptionStorage
    {
        #region ISubscriptionStorage Members

		/// <summary>
		/// Gets all messages from the subscription store.
		/// </summary>
		/// <returns>A list of all the messages in the subscription store.</returns>
        public IList<TransportMessage> GetAllMessages()
        {
            Message[] TransportMessages = this.q.GetAllMessages();

            IList<TransportMessage> result = new List<TransportMessage>(TransportMessages.Length);
            foreach (Message m in TransportMessages)
            {
                TransportMessage toAdd = m.Body as TransportMessage;

                result.Add(toAdd);
            }

            return result;
        }

		/// <summary>
		/// Adds a message to the subscription store.
		/// </summary>
		/// <param name="m">The message to add.</param>
        public void Add(TransportMessage m)
        {
            Message toSend = new Message(m, this.q.Formatter);
            toSend.Recoverable = true;

            this.q.Send(toSend, GetTransactionType());

            this.AddToLookup(m, toSend.Id);
        }

		/// <summary>
		/// Removes a message from the subscription store.
		/// </summary>
		/// <param name="m">The message to remove.</param>
        public void Remove(TransportMessage m)
        {
            string messageId;

            lock (this.lookup)
            {
                if (!this.lookup.ContainsKey(m.ReturnAddress))
                    return;

                this.lookup[m.ReturnAddress].TryGetValue(((SubscriptionMessage)m.Body[0]).typeName, out messageId);

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

                this.q.Formatter = new BinaryMessageFormatter();

                this.q.MessageReadPropertyFilter = mpf;

                this.Init();
            }
        }

        #endregion

        #region helper methods

		/// <summary>
		/// Adds a message to the lookup to find message from
		/// subscriber, to message type, to message id
		/// </summary>
		/// <param name="m">The message to add to the lookup.</param>
		/// <param name="messageId">The id of the message.</param>
        private void AddToLookup(TransportMessage m, string messageId)
        {
            lock (this.lookup)
            {
                if (!this.lookup.ContainsKey(m.ReturnAddress))
                    this.lookup.Add(m.ReturnAddress, new Dictionary<string, string>());

                if (!this.lookup[m.ReturnAddress].ContainsKey(((SubscriptionMessage)m.Body[0]).typeName))
                    this.lookup[m.ReturnAddress].Add(((SubscriptionMessage)m.Body[0]).typeName, messageId);
            }
        }

		/// <summary>
		/// Initializes the lookup from the queue.
		/// </summary>
        private void Init()
        {
            IList<TransportMessage> messages = this.GetAllMessages();

            foreach (TransportMessage m in messages)
                this.AddToLookup(m, m.Id);
        }

        #endregion

        #region members

        private MessageQueue q;

        /// <summary>
        /// lookup from subscriber, to message type, to message id
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> lookup = new Dictionary<string, Dictionary<string, string>>();

        #endregion
    }
}
