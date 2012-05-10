﻿namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Transactions;
    using Unicast;
    using Unicast.Queuing;
    using Unicast.Transport;
    using log4net;

    public class TimeoutRunner : IWantToRunWhenTheBusStarts
    {
        public IManageTimeouts TimeoutManager { get; set; }

        public IPersistTimeouts Persister { get; set; }
        public ISendMessages MessageSender { get; set; }


        public void Run()
        {
            if (TimeoutManager == null)
                return;

            TimeoutManager.SagaTimedOut +=
                (o, e) =>
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.Required))
                    {
                        MessageSender.Send(MapToTransportMessage(e), e.Destination);
                        Persister.Remove(e);

                        scope.Complete();
                    }
                };

            Persister.GetAll().ToList().ForEach(td =>
                TimeoutManager.PushTimeout(td));

            thread = new Thread(Poll) { IsBackground = true };
            thread.Start();
        }

        TransportMessage MapToTransportMessage(TimeoutData timeoutData)
        {
            var transportMessage = new TransportMessage
                                       {
                                           ReplyToAddress = Address.Local,
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
            else
            {
                //we do this to be backwards compatible, this can be removed when going to 3.1.X
                transportMessage.Headers[Headers.Expire] = timeoutData.Time.ToWireFormattedString();

                if (timeoutData.SagaId != Guid.Empty)
                    transportMessage.Headers[Headers.SagaId] = timeoutData.SagaId.ToString();

            }

            return transportMessage;
        }

        public void Stop()
        {
            stopRequested = true;
        }

        void Poll()
        {
            while (!stopRequested)
            {
                try
                {
                    TimeoutManager.PopTimeout();
                }
                catch (Exception ex)
                {
                    //intentionally swallow here to avoid this bringing the entire endpoint down.
                    //remove this when our sattelite support is introduced
                    Logger.ErrorFormat("Failed to pop timeouts - " +  ex);
                }
            }
        }


        Thread thread;
        volatile bool stopRequested;
        static ILog Logger = LogManager.GetLogger("Timeouts");
    }
}


