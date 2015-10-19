namespace NServiceBus.Routing.Legacy
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Features;
    using NServiceBus.Extensibility;
    using NServiceBus.Logging;
    using NServiceBus.Routing;
    using Transports;
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

        protected override Task OnStart(IBusContext busContext)
        {
            SendReadyMessage(initialCapacity, true);
            Logger.DebugFormat("Ready startup message with WorkerSessionId {0} sent. ", workerSessionId);
            return Task.FromResult(0);
        }

        protected override void OnStop()
        {
        }

        void SendReadyMessage(int capacity, bool isStarting)
        {
            //we use the actual address to make sure that the worker inside the master node will check in correctly
            var readyMessage = ControlMessageFactory.Create(MessageIntentEnum.Send);

            readyMessage.Headers.Add(DistributorHeaders.WorkerCapacityAvailable, capacity.ToString());
            readyMessage.Headers.Add(DistributorHeaders.WorkerSessionId, workerSessionId);
            readyMessage.Headers.Add(Headers.ReplyToAddress, receiveAddress);

            if (isStarting)
            {
                readyMessage.Headers.Add(DistributorHeaders.WorkerStarting, bool.TrueString);
            }

            var dispatchOptions = new DispatchOptions(new UnicastAddressTag(distributorControlAddress), DispatchConsistency.Default);
            dispatcher.Dispatch(new[] { new TransportOperation(readyMessage, dispatchOptions) }, new ContextBag()).GetAwaiter().GetResult();
        }
        
        public void MessageProcessed(Dictionary<string, string> headers)
        {
            //if there was a failure this "send" will be rolled back
            string messageSessionId;
            headers.TryGetValue(DistributorHeaders.WorkerSessionId, out messageSessionId);
            if (messageSessionId == null)
            {
                return;
            }
            var messageId = headers[Headers.MessageId];
            Logger.DebugFormat("Got message with id {0} and messageSessionId {1}. WorkerSessionId is {2}", messageId, messageSessionId, workerSessionId);
            if (messageSessionId != workerSessionId)
            {
                Logger.InfoFormat("SKIPPED Ready message for message with id {0} because of sessionId mismatch. MessageSessionId {1}, WorkerSessionId {2}", messageId, messageSessionId, workerSessionId);
            }
            else
            {
                SendReadyMessage(1, false);
            }
        }

        string distributorControlAddress;
        IDispatchMessages dispatcher;
        readonly string receiveAddress;
        int initialCapacity;
        string workerSessionId = Guid.NewGuid().ToString();
        static readonly ILog Logger = LogManager.GetLogger(typeof(ReadyMessageSender));
    }
}
