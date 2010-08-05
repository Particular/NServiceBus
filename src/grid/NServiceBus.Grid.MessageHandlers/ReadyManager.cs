using System;
using NServiceBus.Grid.Messages;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Grid.MessageHandlers
{
    /// <summary>
    /// Manages ready messages
    /// </summary>
    public class ReadyManager : IManageReadyMessages
    {
        /// <summary>
        /// Injected transport
        /// </summary>
        public ITransport Transport
        {
            get { return transport; }
            set 
            { 
                transport = value;
                transport.FinishedMessageProcessing += transport_FinishedMessageProcessing;
            }
        }

        private ITransport transport;

        /// <summary>
        /// Injected Bus
        /// </summary>
        public IBus Bus { get; set; }

        /// <summary>
        /// Address of the distributor's control queue
        /// </summary>
        public virtual string DistributorControlAddress { get; set; }

        /// <summary>
        /// Sends a ready message to the distributor
        /// </summary>
        /// <param name="startup"></param>
        public void SendReadyMessage(bool startup)
        {
            if (DistributorControlAddress == null)
                return;

            if (!canSendReadyMessages)
                return;

            IMessage[] messages;
            if (startup)
            {
                messages = new IMessage[Transport.NumberOfWorkerThreads];
                for (var i = 0; i < Transport.NumberOfWorkerThreads; i++)
                {
                    var rm = new ReadyMessage
                    {
                        ClearPreviousFromThisAddress = (i == 0)
                    };

                    messages[i] = rm;
                }
            }
            else
            {
                messages = new IMessage[1];
                messages[0] = new ReadyMessage();
            }

            Bus.Send(DistributorControlAddress, messages);
        }

        void transport_FinishedMessageProcessing(object sender, EventArgs e)
        {
            if (!_skipSendingReadyMessageOnce)
                SendReadyMessage(false);

            _skipSendingReadyMessageOnce = false;
        }

        void IManageReadyMessages.StopSendingReadyMessages()
        {
            canSendReadyMessages = false;
        }

        void IManageReadyMessages.ContinueSendingReadyMessages()
        {
            canSendReadyMessages = true;
        }

        void IManageReadyMessages.SkipSendingReadyMessageOnce()
        {
            _skipSendingReadyMessageOnce = true;
        }

        /// <summary>
        /// Accessed by multiple threads.
        /// </summary>
        private volatile bool canSendReadyMessages = true;

        /// <summary>
        /// ThreadStatic
        /// </summary>
        [ThreadStatic]
        private static bool _skipSendingReadyMessageOnce;
    }
}
