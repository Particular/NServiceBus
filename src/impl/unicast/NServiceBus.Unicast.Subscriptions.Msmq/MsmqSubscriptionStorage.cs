using System;
using System.Collections.Generic;
using System.Text;
using NServiceBus.Unicast.Transport;
using System.Messaging;
using NServiceBus.Unicast.Transport.Msmq;

namespace NServiceBus.Unicast.Subscriptions.Msmq
{
    public class MsmqSubscriptionStorage : ISubscriptionStorage
    {
        #region ISubscriptionStorage Members

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
        public bool DontUseExternalTransaction
        {
            get { return dontUseExternalTransaction; }
            set { dontUseExternalTransaction = value; }
        }

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
