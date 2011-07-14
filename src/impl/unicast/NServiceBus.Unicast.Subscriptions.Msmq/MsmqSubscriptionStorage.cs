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
using System.Linq;
using System.Messaging;
using Common.Logging;
using System.Transactions;
using NServiceBus.Utils;

namespace NServiceBus.Unicast.Subscriptions.Msmq
{
	/// <summary>
	/// Provides functionality for managing message subscriptions
	/// using MSMQ.
	/// </summary>
    public class MsmqSubscriptionStorage : ISubscriptionStorage
    {
        void ISubscriptionStorage.Init()
        {
            string path = MsmqUtilities.GetFullPath(Queue);

            q = new MessageQueue(path);

            bool transactional;
            try
            {
                transactional = q.Transactional;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(string.Format("There is a problem with the subscription storage queue {0}. See enclosed exception for details.", Queue), ex);
            }

            if (!transactional)
                throw new ArgumentException("Queue must be transactional (" + Queue + ").");

            var mpf = new MessagePropertyFilter();
            mpf.SetAll();

            q.Formatter = new XmlMessageFormatter(new[] { typeof(string) });

            q.MessageReadPropertyFilter = mpf;

            foreach (var m in q.GetAllMessages())
            {
                var subscriber = Address.Parse(m.Label);
                var messageType = m.Body as string;

                entries.Add(new Entry { MessageType = messageType, Subscriber = subscriber});
                AddToLookup(subscriber, messageType, m.Id);
            }
        }

        IEnumerable<string> ISubscriptionStorage.GetSubscribersForMessage(IEnumerable<string> messageTypes)
        {
            return ((ISubscriptionStorage) this).GetSubscriberAddressesForMessage(messageTypes)
                .Select(a => a.ToString());
        }

        IEnumerable<Address> ISubscriptionStorage.GetSubscriberAddressesForMessage(IEnumerable<string> messageTypes)
        {
            var result = new List<Address>();

            lock (locker)
                foreach (var e in entries)
                    foreach (var m in messageTypes)
                        if (e.MessageType == m)
                            if (!result.Contains(e.Subscriber))
                                result.Add(e.Subscriber);

            return result;
        }

        /// <summary>
        /// Checks if configuration is wrong - endpoint isn't transactional and
        /// object isn't configured to handle own transactions.
        /// </summary>
        /// <returns></returns>
        private bool ConfigurationIsWrong()
        {
            return (Transaction.Current == null && !DontUseExternalTransaction);                
        }

        void ISubscriptionStorage.Subscribe(string client, IEnumerable<string> messageTypes)
        {
            ((ISubscriptionStorage)this).Subscribe(Address.Parse(client), messageTypes);
        }

        void ISubscriptionStorage.Subscribe(Address address, IEnumerable<string> messageTypes)
        {
            lock (locker)
            {
                foreach (var m in messageTypes)
                {
                    bool found = false;
                    foreach (var e in entries)
                        if (e.MessageType == m && e.Subscriber == address)
                        {
                            found = true;
                            break;
                        }

                    if (!found)
                    {
                        Add(address, m);

                        entries.Add(new Entry { MessageType = m, Subscriber = address });

                        log.Debug("Subscriber " + address + " added for message " + m + ".");
                    }
                }
            }
        }

        void ISubscriptionStorage.Unsubscribe(string client, IEnumerable<string> messageTypes)
        {
            ((ISubscriptionStorage)this).Unsubscribe(Address.Parse(client), messageTypes);   
        }

        void ISubscriptionStorage.Unsubscribe(Address address, IEnumerable<string> messageTypes)
        {
            lock (locker)
            {
                foreach (var e in entries.ToArray())
                    foreach (var m in messageTypes)
                        if (e.MessageType == m && e.Subscriber == address)
                        {
                            Remove(address, m);

                            entries.Remove(e);

                            log.Debug("Subscriber " + address + " removed for message " + m + ".");
                        }
            }
        }

		/// <summary>
		/// Adds a message to the subscription store.
		/// </summary>
        public void Add(Address subscriber, string typeName)
        {
		    var toSend = new Message {Formatter = q.Formatter, Recoverable = true, Label = subscriber.ToString(), Body = typeName};

		    q.Send(toSend, GetTransactionType());

            AddToLookup(subscriber, typeName, toSend.Id);
        }

		/// <summary>
		/// Removes a message from the subscription store.
		/// </summary>
        public void Remove(Address subscriber, string typeName)
        {
			var messageId = RemoveFromLookup(subscriber, typeName);

			if (messageId == null)
				return;

		    q.ReceiveById(messageId, GetTransactionType());
        }

        /// <summary>
        /// Returns the transaction type (automatic or single) that should be used
        /// based on the configuration of enlisting into external transactions.
        /// </summary>
        /// <returns></returns>
	    private MessageQueueTransactionType GetTransactionType()
	    {
            if (ConfigurationIsWrong())
                throw new InvalidOperationException("This endpoint is not configured to be transactional. Processing subscriptions on a non-transactional endpoint is not supported by default. If you still wish to do so, please set the 'DontUseExternalTransaction' property of MsmqSubscriptionStorage to 'true'.\n\nThe recommended solution to this problem is to include '.IsTransaction(true)' after '.MsmqTransport()' in your fluent initialization code, or if you're using NServiceBus.Host.exe to have the class which implements IConfigureThisEndpoint to also inherit AsA_Server or AsA_Publisher.");

	        var t = MessageQueueTransactionType.Automatic;
	        if (DontUseExternalTransaction)
	            t = MessageQueueTransactionType.Single;
	        return t;
	    }

	    #region config info

		/// <summary>
		/// Gets/sets whether or not to use a trasaction started outside the 
		/// subscription store.
		/// </summary>
        public virtual bool DontUseExternalTransaction { get; set; }

		/// <summary>
		/// Sets the address of the queue where subscription messages will be stored.
		/// For a local queue, just use its name - msmq specific info isn't needed.
		/// </summary>
        public Address Queue
        {
            get; set;
        }

        #endregion

        #region helper methods

		/// <summary>
		/// Adds a message to the lookup to find message from
		/// subscriber, to message type, to message id
		/// </summary>
        private void AddToLookup(Address subscriber, string typeName, string messageId)
        {
            lock (lookup)
            {
                if (!lookup.ContainsKey(subscriber))
                    lookup.Add(subscriber, new Dictionary<string, string>());

                if (!lookup[subscriber].ContainsKey(typeName))
                    lookup[subscriber].Add(typeName, messageId);
            }
        }

		private string RemoveFromLookup(Address subscriber, string typeName)
		{
			string messageId = null;
			lock (lookup)
			{
				Dictionary<string, string> endpoints;
				if (lookup.TryGetValue(subscriber, out endpoints))
				{
					if (endpoints.TryGetValue(typeName, out messageId))
					{
						endpoints.Remove(typeName);
						if (endpoints.Count == 0)
						{
							lookup.Remove(subscriber);
						}
					}
				}
			}
			return messageId;
		}

        #endregion

        #region members

        private MessageQueue q;

        /// <summary>
        /// lookup from subscriber, to message type, to message id
        /// </summary>
        private readonly Dictionary<Address, Dictionary<string, string>> lookup = new Dictionary<Address, Dictionary<string, string>>();

        private readonly List<Entry> entries = new List<Entry>();
        private readonly object locker = new object();

	    private readonly ILog log = LogManager.GetLogger(typeof(ISubscriptionStorage));

        #endregion
    }
}
