namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Faults;
    using ObjectBuilder;
    using Unicast;
    using Unicast.Queuing;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport;
    using Unicast.Transport.Transactional;

    public class TimeoutDispatcherProcessor : IWantToRunWhenTheBusStarts, IDisposable
    {
        TimeoutPersisterReceiver timeoutPersisterReceiver;
        ITransport inputTransport;

        public static readonly Address TimeoutDispatcherAddress;

        public ISendMessages MessageSender { get; set; }

        public IPersistTimeouts TimeoutsPersister { get; set; }

        public TransactionalTransport MainTransport { get; set; }

        public IBuilder Builder { get; set; }

        static TimeoutDispatcherProcessor()
        {
            TimeoutDispatcherAddress = Address.Parse(Configure.EndpointName).SubScope("TimeoutsDispatcher");
        }

        public void Run()
        {
            if (!Configure.Instance.IsTimeoutManagerEnabled())
            {
                return;
            }

            timeoutPersisterReceiver = new TimeoutPersisterReceiver(Builder.Build<IManageTimeouts>());
            timeoutPersisterReceiver.MessageSender = MessageSender;
            timeoutPersisterReceiver.TimeoutsPersister = TimeoutsPersister;
            timeoutPersisterReceiver.Start();

            inputTransport = new TransactionalTransport
                {
                    MessageReceiver = new MsmqMessageReceiver(),
                    IsTransactional = true,
                    NumberOfWorkerThreads = MainTransport.NumberOfWorkerThreads == 0 ? 1 : MainTransport.NumberOfWorkerThreads,
                    MaxRetries = MainTransport.MaxRetries,
                    FailureManager = Builder.Build(MainTransport.FailureManager.GetType()) as IManageMessageFailures //Need to change this!
                };

            inputTransport.TransportMessageReceived += OnTransportMessageReceived;

            inputTransport.Start(TimeoutDispatcherAddress);
        }

        private void OnTransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            var transportMessage = e.Message;
            var timeoutId = transportMessage.Headers["Timeout.Id"];
            TimeoutData timeoutData;

            if(TimeoutsPersister.TryRemove(timeoutId, out timeoutData))
            {
                MessageSender.Send(MapToTransportMessage(timeoutData), timeoutData.Destination);
            }
        }

        private const string OriginalReplyToAddress = "NServiceBus.Timeout.ReplyToAddress";

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

            return transportMessage;
        }

        public void Dispose()
        {
            timeoutPersisterReceiver.Stop();

            if (inputTransport != null)
            {
                inputTransport.Dispose();
            }
        }
    }
}