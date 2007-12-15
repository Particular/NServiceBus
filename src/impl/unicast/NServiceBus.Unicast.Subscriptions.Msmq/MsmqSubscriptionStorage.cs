using System;
using System.Collections.Generic;
using System.Text;
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
        public IList<Msg> GetAllMessages()
        {
            Message[] msgs = this.q.GetAllMessages();

            IList<Msg> result = new List<Msg>(msgs.Length);
            foreach (Message m in msgs)
            {
                Msg toAdd = m.Body as Msg;

                result.Add(toAdd);
            }

            return result;
        }

		/// <summary>
		/// Adds a message to the subscription store.
		/// </summary>
		/// <param name="m">The message to add.</param>
        public void Add(Msg m)
        {
            Message toSend = new Message(m, this.q.Formatter);
            toSend.Recoverable = true;

            MessageQueueTransactionType t = MessageQueueTransactionType.Automatic;
            if (this.dontUseExternalTransaction)
                t = MessageQueueTransactionType.Single;

            this.q.Send(toSend, t);

            this.AddToLookup(m, toSend.Id);
        }

		/// <summary>
		/// Removes a message from the subscription store.
		/// </summary>
		/// <param name="m">The message to remove.</param>
        public void Remove(Msg m)
        {
            string messageId = null;

            lock (this.lookup)
            {
                if (!this.lookup.ContainsKey(m.ReturnAddress))
                    return;

                this.lookup[m.ReturnAddress].TryGetValue(((SubscriptionMessage)m.Body[0]).typeName, out messageId);

                if (messageId == null)
                    return;
            }

            MessageQueueTransactionType t = MessageQueueTransactionType.Automatic;
            if (this.dontUseExternalTransaction)
                t = MessageQueueTransactionType.Single;

            this.q.ReceiveById(messageId, t);
        }

        #endregion

        #region config info

        private bool dontUseExternalTransaction;

		/// <summary>
		/// Gets/sets whether or not to use a trasaction started outside the 
		/// subscription store.
		/// </summary>
        public bool DontUseExternalTransaction
        {
            get { return dontUseExternalTransaction; }
            set { dontUseExternalTransaction = value; }
        }

		/// <summary>
		/// Sets the address of the MSMQ queue where subscription messages
		/// will be stored.
		/// </summary>
        public string Queue
        {
            set
            {
                if (!MsmqTransport.QueueIsLocal(value))
                    throw new ArgumentException("Queue must be local (" + value + ").");
                else
                {
                    q = new MessageQueue(value);

                    if (!q.Transactional)
                        throw new ArgumentException("Queue must be transactional (" + value + ").");
                }

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
        private void AddToLookup(Msg m, string messageId)
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
            IList<Msg> messages = this.GetAllMessages();

            foreach (Msg m in messages)
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
