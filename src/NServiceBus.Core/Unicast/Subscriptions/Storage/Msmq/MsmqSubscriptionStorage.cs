namespace NServiceBus.Unicast.Subscriptions.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Transactions;
    using Logging;
    using Settings;
    using Transports.Msmq;
    using MessageType = Subscriptions.MessageType;

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

            if (!transactional && SettingsHolder.Get<bool>("Transactions.Enabled"))
                throw new ArgumentException("Queue must be transactional (" + Queue + ").");

            var mpf = new MessagePropertyFilter();
            mpf.SetAll();

            q.Formatter = new XmlMessageFormatter(new[] { typeof(string) });

            q.MessageReadPropertyFilter = mpf;

            foreach (var m in q.GetAllMessages())
            {
                var subscriber = Address.Parse(m.Label);
                var messageTypeString = m.Body as string;
                var messageType = new MessageType(messageTypeString); //this will parse both 2.6 and 3.0 type strings

                entries.Add(new Entry { MessageType = messageType, Subscriber = subscriber });
                AddToLookup(subscriber, messageType, m.Id);
            }
        }


        IEnumerable<Address> ISubscriptionStorage.GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
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


        void ISubscriptionStorage.Subscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            lock (locker)
            {
                foreach (var messageType in messageTypes)
                {
                    bool found = false;
                    foreach (var e in entries)
                        if (e.MessageType == messageType && e.Subscriber == address)
                        {
                            found = true;
                            break;
                        }

                    if (!found)
                    {
                        Add(address, messageType);

                        entries.Add(new Entry { MessageType = messageType, Subscriber = address });

                        log.Debug("Subscriber " + address + " added for message " + messageType + ".");
                    }
                }
            }
        }

       
        void ISubscriptionStorage.Unsubscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            lock (locker)
            {
                foreach (var e in entries.ToArray())
                    foreach (var messageType in messageTypes)
                        if (e.MessageType == messageType && e.Subscriber == address)
                        {
                            Remove(address, messageType);

                            entries.Remove(e);

                            log.Debug("Subscriber " + address + " removed for message " + messageType + ".");
                        }
            }
        }

		/// <summary>
		/// Adds a message to the subscription store.
		/// </summary>
        public void Add(Address subscriber, MessageType messageType)
        {
		    var toSend = new Message {Formatter = q.Formatter, Recoverable = true, Label = subscriber.ToString(), Body = messageType.TypeName +  ", Version=" + messageType.Version};

		    q.Send(toSend, GetTransactionType());

            AddToLookup(subscriber, messageType, toSend.Id);
        }

		/// <summary>
		/// Removes a message from the subscription store.
		/// </summary>
        public void Remove(Address subscriber, MessageType messageType)
        {
            var messageId = RemoveFromLookup(subscriber, messageType);

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
            if (!SettingsHolder.Get<bool>("Transactions.Enabled"))
            {
                return MessageQueueTransactionType.None;
            }

            if (ConfigurationIsWrong())
            {
                throw new InvalidOperationException("This endpoint is not configured to be transactional. Processing subscriptions on a non-transactional endpoint is not supported by default. If you still wish to do so, please set the 'DontUseExternalTransaction' property of MsmqSubscriptionStorage to 'true'.\n\nThe recommended solution to this problem is to include '.IsTransaction(true)' after '.MsmqTransport()' in your fluent initialization code, or if you're using NServiceBus.Host.exe to have the class which implements IConfigureThisEndpoint to also inherit AsA_Server or AsA_Publisher.");
            }

	        var t = MessageQueueTransactionType.Automatic;
	        if (DontUseExternalTransaction)
	        {
	            t = MessageQueueTransactionType.Single;
	        }

	        return t;
	    }


		/// <summary>
		/// Gets/sets whether or not to use a transaction started outside the 
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


		/// <summary>
		/// Adds a message to the lookup to find message from
		/// subscriber, to message type, to message id
		/// </summary>
        private void AddToLookup(Address subscriber, MessageType typeName, string messageId)
        {
            lock (lookup)
            {
                if (!lookup.ContainsKey(subscriber))
                    lookup.Add(subscriber, new Dictionary<MessageType, string>());

                if (!lookup[subscriber].ContainsKey(typeName))
                    lookup[subscriber].Add(typeName, messageId);
            }
        }

		string RemoveFromLookup(Address subscriber, MessageType typeName)
		{
			string messageId = null;
			lock (lookup)
			{
                Dictionary<MessageType, string> endpoints;
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

     
        MessageQueue q;

        /// <summary>
        /// lookup from subscriber, to message type, to message id
        /// </summary>
        readonly Dictionary<Address, Dictionary<MessageType, string>> lookup = new Dictionary<Address, Dictionary<MessageType, string>>();

        readonly List<Entry> entries = new List<Entry>();
        readonly object locker = new object();

	    readonly ILog log = LogManager.GetLogger(typeof(ISubscriptionStorage));
    }
}
