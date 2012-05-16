using NServiceBus.Faults;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Queuing.Msmq;
using NServiceBus.Unicast.Transport;
using NServiceBus.Unicast.Transport.Transactional;

namespace NServiceBus.Satellites
{
    public interface ISatelliteTransportBuilder
    {        
        ITransport Build(int numberOfWorkerThreads, int maxRetries, bool isTransactional);
    }

    public class SatelliteTransportBuilder : ISatelliteTransportBuilder
    {
        public IBuilder Builder { get; set; }
        public TransactionalTransport MainTransport { get; set; }

        public ITransport Build(int numberOfWorkerThreads, int maxRetries, bool isTransactional)
        {
            var nt = numberOfWorkerThreads > 0 ? numberOfWorkerThreads : MainTransport.NumberOfWorkerThreads == 0 ? 1 : MainTransport.NumberOfWorkerThreads;
            var mr = maxRetries > 0 ? maxRetries : MainTransport.MaxRetries;

            return new TransactionalTransport
            {
                MessageReceiver = new MsmqMessageReceiver(),
                IsTransactional = isTransactional,
                NumberOfWorkerThreads = nt,
                MaxRetries = mr,
                FailureManager = Builder.Build(MainTransport.FailureManager.GetType()) as IManageMessageFailures
            };            
        }
    }
}