namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using System.Transactions;
    using Core;
    using Features;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using Satellites;
    using Transports;
    using Unicast.Transport;

    public class TimeoutDispatcherProcessor : IAdvancedSatellite
    {  
        public ISendMessages MessageSender { get; set; }

        public IPersistTimeouts TimeoutsPersister { get; set; }
        
        public TimeoutPersisterReceiver TimeoutPersisterReceiver { get; set; }

        public PipelineExecutor PipelineExecutor { get; set; }

        public Address InputAddress
        {
            get
            {
                return TimeoutManager.DispatcherAddress;
            }
        }

        public bool Disabled
        {
            get { return !Feature.IsEnabled<TimeoutManager>(); }
        }

        public bool Handle(TransportMessage message)
        {
            var timeoutId = message.Headers["Timeout.Id"];
            

            var persisterV2 = TimeoutsPersister as IPersistTimeoutsV2;
            if (persisterV2 != null)
            {
                var timeoutData = persisterV2.Peek(timeoutId);
                if (timeoutData == null)
                {
                    return true;
                }

                if (ShouldSuppressTransaction())
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.Suppress))
                    {
                        try
                        {
                            PipelineExecutor.CurrentContext.Set("do-not-enlist-in-native-transaction", true);
                            MessageSender.Send(timeoutData.ToTransportMessage(), timeoutData.Destination);
                        }
                        finally
                        {
                            PipelineExecutor.CurrentContext.Set("do-not-enlist-in-native-transaction", false);
                        }

                        scope.Complete();
                    }
                }
                else
                {
                    MessageSender.Send(timeoutData.ToTransportMessage(), timeoutData.Destination);
                }

                return persisterV2.TryRemove(timeoutId);
            }
            else
            {
                TimeoutData timeoutData;
                if (TimeoutsPersister.TryRemove(timeoutId, out timeoutData))
                {
                    MessageSender.Send(timeoutData.ToTransportMessage(), timeoutData.Destination);
                }
            }

            return true;
        }

        public void Start()
        {
            TimeoutPersisterReceiver.Start();
        }

        public void Stop()
        {
            TimeoutPersisterReceiver.Stop();
        }

        public Action<TransportReceiver> GetReceiverCustomization()
        {
            return receiver =>
            {
                //TODO: The line below needs to change when we refactor the slr to be:
                // transport.DisableSLR() or similar
                receiver.FailureManager = new ManageMessageFailuresWithoutSlr(receiver.FailureManager);
            };
        }

        bool ShouldSuppressTransaction()
        {
            var suppressDtc = SettingsHolder.Get<bool>("Transactions.SuppressDistributedTransactions");
            return !IsTransportSupportingDtc() || suppressDtc;
        }

        bool IsTransportSupportingDtc()
        {
            var selectedTransport = SettingsHolder.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport");
            if (selectedTransport.HasSupportForDistributedTransactions.HasValue)
            {
                return selectedTransport.HasSupportForDistributedTransactions.Value;
            }

            return !selectedTransport.GetType().Name.Contains("RabbitMQ");
        }
    }
}
