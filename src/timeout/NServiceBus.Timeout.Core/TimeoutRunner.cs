namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Transactions;
    using Saga;
    using Unicast;
    using Unicast.Queuing;
    using Unicast.Transport;
    using Common.Logging;

    public class TimeoutRunner : IWantToRunWhenTheBusStarts
    {
        private ILog Logger = LogManager.GetLogger(typeof(TimeoutRunner));

        public IManageTimeouts TimeoutManager { get; set; }

        public IPersistTimeouts Persister { get; set; }
        public ISendMessages MessageSender { get; set; }

        public void Run()
        {
            if (TimeoutManager == null)
                return;

            TimeoutManager.TimedOut += (o, e) =>
                                           {
                                               try
                                               {
                                                   OnTimeout(e, true);
                                               }
                                               catch(Exception ex)
                                               {
                                                   Logger.Error("Failed to handle timeout. Waiting 1 second before continuing.", ex);
                                                   Thread.Sleep(TimeSpan.FromSeconds(1));
                                               }
                                           };
            TimeoutManager.TimeOutCleared += (o, e) => OnTimeout(e, false);

            Persister.GetAll().ToList().ForEach(td =>
                TimeoutManager.PushTimeout(td));

            thread = new Thread(Poll);
            thread.IsBackground = true;
            thread.Start();
        }

        private void OnTimeout(TimeoutData e, bool sendMessage)
        {
            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required))
                {
                    if (sendMessage)
                        MessageSender.Send(MapToTransportMessage(e), e.Destination);
                    Persister.RemoveTimeout(e.Id);

                    scope.Complete();
                }
            }
            catch
            {
                // in this case the change is not persisted, so we should return it back to the manager
                // to avoid any inconsistency between persisted store and in-memory store.
                TimeoutManager.PushTimeout(e);
                throw;
            }
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

            if(timeoutData.Headers != null)
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
                TimeoutManager.PopTimeout();
        }

        
        Thread thread;
        volatile bool stopRequested;

    }
}


