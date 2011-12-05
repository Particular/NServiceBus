namespace NServiceBus.Timeout.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Transactions;
    using Saga;
    using Unicast;
    using Unicast.Queuing;
    using Unicast.Transport;

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
                        Persister.Remove(e.SagaId);

                        scope.Complete();
                    }
                };

            Persister.GetAll().ToList().ForEach(td =>
                TimeoutManager.PushTimeout(td));

            thread = new Thread(Poll);
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
                                           Body = timeoutData.State
                                       };

            transportMessage.Headers[NServiceBus.Headers.Expire] = timeoutData.Time.ToString();
            transportMessage.Headers[NServiceBus.Headers.SagaId] = timeoutData.SagaId.ToString();

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


