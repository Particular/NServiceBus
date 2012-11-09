namespace NServiceBus.Unicast.Transport.Transactional.ThreadingStrategies
{
    using System;

    public class PooledThreadingStrategy : IThreadingStrategy
    {
        public void ChangeMaxDegreeOfParallelism(int value)
        {
            throw new NotImplementedException();
        }

      
        public void Start(int maxDegreeOfParallelism,Action workerMethod)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}