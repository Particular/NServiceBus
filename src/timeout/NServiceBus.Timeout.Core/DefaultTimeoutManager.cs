namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Transactions;
    using Unicast.Queuing;
    using Unicast.Transport;
    using Utils;
    using log4net;

    public class DefaultTimeoutManager : IManageTimeouts
    {
        public const string OriginalReplyToAddress = "NServiceBus.Timeout.ReplyToAddress";
        static readonly ILog Logger = LogManager.GetLogger("DefaultTimeoutManager");

        readonly Thread workerThread;
        readonly object lockObject = new object();
        private volatile bool stopRequested;
        private DateTime nextRetrieval = DateTime.UtcNow;
        private volatile bool timeoutPushed;

        public IPersistTimeouts TimeoutsPersister { get; set; }

        public ISendMessages MessageSender { get; set; }

        public DefaultTimeoutManager()
        {
            workerThread = new Thread(Poll) { IsBackground = true };
        }

        public void DispatchTimeout(TimeoutData timeoutData)
        {
            var message = MapToTransportMessage(timeoutData);

            MessageSender.Send(message, timeoutData.Destination);
        }

        public void PushTimeout(TimeoutData timeout)
        {
            TimeoutsPersister.Add(timeout);

            lock (lockObject)
            {
                if (nextRetrieval > timeout.Time)
                {
                    nextRetrieval = timeout.Time;
                }
                timeoutPushed = true;
            }
        }

        public void RemoveTimeout(string timeoutId)
        {
            TimeoutsPersister.Remove(timeoutId);
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            TimeoutsPersister.RemoveTimeoutBy(sagaId);
        }

        public void Start()
        {
            stopRequested = false;
            workerThread.Start();
        }

        public void Stop()
        {
            stopRequested = true;
            workerThread.Join();
        }

        void Poll()
        {
            var transactionWrapper = new TransactionWrapper();

            while (!stopRequested)
            {
                if (nextRetrieval.AddMilliseconds(-200) > DateTime.UtcNow)
                {
                    Thread.Sleep(1);
                    continue;
                }

                try
                {
                    DateTime nextExpiredTimeout;
                    var timeoutDatas = TimeoutsPersister.GetNextChunk(out nextExpiredTimeout);

                    //RavenDB 616 doesn't work with Parallel, but in v4 (Raven960) we should be able to use it :)  
                    //Parallel.ForEach(timeoutDatas, timeoutData =>
                    foreach (var timeoutData in timeoutDatas)
                        {
                            if (timeoutData.Time <= DateTime.UtcNow)
                            {
                                var data = timeoutData;
                                transactionWrapper.RunInTransaction(() =>
                                    {
                                        RemoveTimeout(data.Id);
                                        DispatchTimeout(data);
                                    }, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(60));
                            }
                        }
                    //);

                    lock (lockObject)
                    {
                        //Check if nextRetrieval has been modified (This means that a push come in) and if it has check if it is earlier than nextExpiredTimeout time
                        if (!timeoutPushed && nextExpiredTimeout > nextRetrieval)
                        {
                            nextRetrieval = nextExpiredTimeout;
                        }
                        timeoutPushed = false;
                    }
                }
                catch (Exception ex)
                {
                    //intentionally swallow here to avoid this bringing the entire endpoint down.
                    //remove this when our sattelite support is introduced
                    Logger.Error("Polling of timeouts failed.", ex);
                }
            }
        }

        static TransportMessage MapToTransportMessage(TimeoutData timeoutData)
        {
            var replyToAddress = Address.Local;
           if(timeoutData.Headers.ContainsKey(OriginalReplyToAddress))
           {
                replyToAddress = Address.Parse(timeoutData.Headers[OriginalReplyToAddress]);
                timeoutData.Headers.Remove(OriginalReplyToAddress);
            }

            var transportMessage = new TransportMessage
                {
                    ReplyToAddress = replyToAddress,
                    Headers = new Dictionary<string, string>(),
                    Recoverable = true,
                    MessageIntent = MessageIntentEnum.Send,
                    CorrelationId = timeoutData.CorrelationId,
                    Body = timeoutData.State
                };

            if (timeoutData.Headers != null)
            {
                transportMessage.Headers = timeoutData.Headers;
            }
            else if (timeoutData.SagaId != Guid.Empty)
            {
                transportMessage.Headers[Headers.SagaId] = timeoutData.SagaId.ToString();
            }

            return transportMessage;
        }
    }
}
