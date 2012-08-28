namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Core;
    using Unicast.Queuing;
    using Unicast.Transport;
    using log4net;

    public class TimeoutPersisterReceiver
    {
        const string OriginalReplyToAddress = "NServiceBus.Timeout.ReplyToAddress";

        static readonly ILog Logger = LogManager.GetLogger("TimeoutPersisterReceiver");

        readonly object lockObject = new object();

        volatile bool stopRequested;
        volatile bool timeoutPushed;
        DateTime nextRetrieval = DateTime.UtcNow;
        readonly Thread workerThread;

        public IPersistTimeouts TimeoutsPersister { get; set; }
        public ISendMessages MessageSender { get; set; }

        public TimeoutPersisterReceiver(IManageTimeouts timeoutsManager)
        {
            timeoutsManager.TimeoutPushed += TimeoutsManagerOnTimeoutPushed;

            workerThread = new Thread(Poll) { IsBackground = true };
        }
        
        private void TimeoutsManagerOnTimeoutPushed(object sender, TimeoutData timeoutData)
        {
            lock (lockObject)
            {
                if (nextRetrieval > timeoutData.Time)
                {
                    nextRetrieval = timeoutData.Time;
                }
                timeoutPushed = true;
            }
        }

        public void Start()
        {
            workerThread.Start();
        }


        public void Stop()
        {
            stopRequested = true;
            workerThread.Join();
        }

        void Poll()
        {
            int pollingFailuresCount = 0;

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

                    foreach (var timeoutData in timeoutDatas)
                    {
                        if (timeoutData.Time <= DateTime.UtcNow)
                        {
                            MessageSender.Send(MapToTransportMessage(timeoutData), TimeoutDispatcherProcessor.TimeoutDispatcherAddress);
                        }
                    }

                    lock (lockObject)
                    {
                        //Check if nextRetrieval has been modified (This means that a push come in) and if it has check if it is earlier than nextExpiredTimeout time
                        if (!timeoutPushed)
                        {
                            nextRetrieval = nextExpiredTimeout;
                        }
                        else if (nextExpiredTimeout < nextRetrieval)
                        {
                            nextRetrieval = nextExpiredTimeout;
                        }

                        timeoutPushed = false;
                    }

                    pollingFailuresCount = 0;
                }
                catch (Exception ex)
                {
                    Logger.Error("Polling of timeouts failed.", ex);

                    if (pollingFailuresCount >= 10)
                    {
                        throw; //This should bring down the whole endpoint
                    }

                    pollingFailuresCount++;
                }
            }
        }

        static TransportMessage MapToTransportMessage(TimeoutData timeoutData)
        {
            var replyToAddress = Address.Local;
            if (timeoutData.Headers != null && timeoutData.Headers.ContainsKey(OriginalReplyToAddress))
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

            //Add extra header so we don't need to convert again to TimeoutData
            transportMessage.Headers["Timeout.Destination"] = timeoutData.Destination.ToString();
            transportMessage.Headers["Timeout.Id"] = timeoutData.Id;

            return transportMessage;
        }
    }
}