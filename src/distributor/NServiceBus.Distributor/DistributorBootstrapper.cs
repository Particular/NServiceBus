using System;
using NServiceBus.Config;
using NServiceBus.Unicast.Distributor;
using NServiceBus.Unicast.Queuing.Msmq;
using NServiceBus.Unicast.Transport.Transactional;

namespace NServiceBus.Distributor
{
    using Unicast;

    public class DistributorBootstrapper : IDisposable, IWantToRunWhenTheBusStarts
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
            if (!ConfigureDistributor.DistributorShouldRunOnThisEndpoint())
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


            distributor.Start();
        }

        Unicast.Distributor.Distributor distributor;


    }
}
