using System;
using NServiceBus.Config;
using NServiceBus.Unicast.Distributor;
using NServiceBus.Unicast.Queuing.Msmq;
using NServiceBus.Unicast.Transport.Transactional;

namespace NServiceBus.Distributor
{
    public class DistributorBootstrapper : IDisposable, IWantToRunWhenConfigurationIsComplete
    {

        public IWorkerAvailabilityManager WorkerAvailabilityManager { get; set; }
        public int NumberOfWorkerThreads { get; set; }
        public Address InputQueue { get; set; }

        public void Dispose()
        {
            if (distributor != null)
                distributor.Stop();
        }

        public void Run()
        {
            if (!RoutingConfig.IsConfiguredAsMasterNode)
                return;

            var dataTransport = new TransactionalTransport
            {
                NumberOfWorkerThreads = NumberOfWorkerThreads,
                IsTransactional = true,
                MessageReceiver = new MsmqMessageReceiver()
            };

            distributor = new Unicast.Distributor.Distributor
            {
                MessageBusTransport = dataTransport,
                MessageSender = new MsmqMessageSender(),
                WorkerManager = WorkerAvailabilityManager,
                DataTransportInputQueue = InputQueue
            };

            var bus = Configure.Instance.Builder.Build<IStartableBus>();
            bus.Started += (obj, ev) => distributor.Start();
        }

        Unicast.Distributor.Distributor distributor;


    }
}
