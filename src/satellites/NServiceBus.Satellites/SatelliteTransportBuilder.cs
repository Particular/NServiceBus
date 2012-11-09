using NServiceBus.Faults;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Transport;
using NServiceBus.Unicast.Transport.Transactional;
using NServiceBus.Config;

namespace NServiceBus.Satellites
{
    public interface ISatelliteTransportBuilder
    {        
        ITransport Build();
    }

    public class SatelliteTransportBuilder : ISatelliteTransportBuilder
    {
        public IBuilder Builder { get; set; }
        public TransactionalTransport MainTransport { get; set; }

        public ITransport Build()
        {
            var nt = 1; // MainTransport != null ? MainTransport.NumberOfWorkerThreads == 0 ? 1 : MainTransport.NumberOfWorkerThreads : 1;

            var fm = MainTransport != null
                         ? Builder.Build(MainTransport.FailureManager.GetType()) as IManageMessageFailures
                         : Builder.Build<IManageMessageFailures>();

            

            var transactionalTransport = new TransactionalTransport
                                             {
                                                 NumberOfWorkerThreads = nt,
                                                 FailureManager = fm,
                                             };


            if (MainTransport != null)
                transactionalTransport.TransactionSettings = MainTransport.TransactionSettings;
            

            return transactionalTransport;
        }
    }
}