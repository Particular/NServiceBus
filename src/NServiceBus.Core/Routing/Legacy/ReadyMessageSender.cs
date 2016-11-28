namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using Features;
    using Logging;
    using Routing;
    using Transport;
    using Unicast.Transport;

    class ReadyMessageSender : FeatureStartupTask
    {
        public ReadyMessageSender(IDispatchMessages dispatcher, string receiveAddress, int initialCapacity, string distributorControlAddress)
        {
            this.initialCapacity = initialCapacity;
            this.distributorControlAddress = distributorControlAddress;
            this.dispatcher = dispatcher;
            this.receiveAddress = receiveAddress;
        }

        protected override Task OnStart(IMessageSession session)
        {
            Logger.DebugFormat("Sending ready startup message with WorkerSessionId {0} sent. ", workerSessionId);

            return SendReadyMessage(initialCapacity, true, new TransportTransaction());
        }

        protected override Task OnStop(IMessageSession session)
        {
            return TaskEx.CompletedTask;
        }

        Task SendReadyMessage(int capacity, bool isStarting, TransportTransaction transaction)
        {
            //we use the actual address to make sure that the worker inside the master node will check in correctly
            var readyMessage = ControlMessageFactory.Create(MessageIntentEnum.Send);

            readyMessage.Headers.Add(LegacyDistributorHeaders.WorkerCapacityAvailable, capacity.ToString());
            readyMessage.Headers.Add(LegacyDistributorHeaders.WorkerSessionId, workerSessionId);
            readyMessage.Headers.Add(Headers.ReplyToAddress, receiveAddress);

            if (isStarting)
            {
                readyMessage.Headers.Add(LegacyDistributorHeaders.WorkerStarting, bool.TrueString);
            }

            var transportOperation = new TransportOperation(readyMessage, new UnicastAddressTag(distributorControlAddress));
            return dispatcher.Dispatch(new TransportOperations(transportOperation), transaction, new ContextBag());
        }

        public Task MessageProcessed(Dictionary<string, string> headers, TransportTransaction transaction)
        {
            //if there was a failure this "send" will be rolled back
            string messageSessionId;
            headers.TryGetValue(LegacyDistributorHeaders.WorkerSessionId, out messageSessionId);
            if (messageSessionId == null)
            {
                return TaskEx.CompletedTask;
            }
            var messageId = headers[Headers.MessageId];
            Logger.DebugFormat("Got message with id {0} and messageSessionId {1}. WorkerSessionId is {2}", messageId, messageSessionId, workerSessionId);
            if (messageSessionId != workerSessionId)
            {
                Logger.InfoFormat("SKIPPED Ready message for message with id {0} because of sessionId mismatch. MessageSessionId {1}, WorkerSessionId {2}", messageId, messageSessionId, workerSessionId);
                return TaskEx.CompletedTask;
            }

            return SendReadyMessage(1, false, transaction);
        }

        readonly string receiveAddress;
        IDispatchMessages dispatcher;

        string distributorControlAddress;
        int initialCapacity;
        string workerSessionId = Guid.NewGuid().ToString();
        static readonly ILog Logger = LogManager.GetLogger(typeof(ReadyMessageSender));
    }
}