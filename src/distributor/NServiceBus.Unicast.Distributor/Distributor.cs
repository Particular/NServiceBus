using System;
using System.Collections.Generic;
using System.Text;
using NServiceBus.Unicast.Transport;
using NServiceBus.Messages;
using System.Threading;
using Common.Logging;

namespace NServiceBus.Unicast.Distributor
{
    public class Distributor
    {
        #region config info

        private ITransport subscriptionInfoTransport;
        public ITransport SubscriptionInfoTransport
        {
            set { subscriptionInfoTransport = value; }
        }

        private ITransport messageBusTransport;
        public ITransport MessageBusTransport
        {
            set { messageBusTransport = value; }
        }

        private IWorkerAvailabilityManager workerManager;
        public IWorkerAvailabilityManager WorkerManager
        {
            set { workerManager = value; }
        }

        #endregion

        #region public methods

        public void Start()
        {
            this.subscriptionInfoTransport.MessageTypesToBeReceived = new List<Type>(new Type[] { typeof(ReadyMessage) });

            this.messageBusTransport.MsgReceived += new EventHandler<MsgReceivedEventArgs>(messageBusTransport_MsgReceived);
            this.subscriptionInfoTransport.MsgReceived += new EventHandler<MsgReceivedEventArgs>(subscriptionInfoTransport_MsgReceived);

            this.subscriptionInfoTransport.Start();
            this.messageBusTransport.Start();
        }

        public void Stop()
        {
        }

        #endregion

        #region helper methods

        void subscriptionInfoTransport_MsgReceived(object sender, MsgReceivedEventArgs e)
        {
            if (e.Message.Body != null)
                if (e.Message.Body.Length == 1)
                    if (e.Message.Body[0] is ReadyMessage)
                    {
                        ReadyMessage rm = e.Message.Body[0] as ReadyMessage;

                        logger.Debug("Server available: " + e.Message.ReturnAddress);

                        if (rm.ClearPreviousFromThisAddress)
                            this.workerManager.ClearAvailabilityForWorker(e.Message.ReturnAddress);

                        this.workerManager.WorkerAvailable(e.Message.ReturnAddress);
                    }
        }

        void messageBusTransport_MsgReceived(object sender, MsgReceivedEventArgs e)
        {
            string destination = this.workerManager.PopAvailableWorker();

            if (destination == null)
                this.messageBusTransport.ReceiveMessageLater(e.Message);
            else
            {
                logger.Debug("Sending message to: " + destination);
                this.messageBusTransport.Send(e.Message, destination);
            }
        }

        #endregion

        #region members

        private static ILog logger = LogManager.GetLogger(typeof(Distributor));

        #endregion
    }
}
