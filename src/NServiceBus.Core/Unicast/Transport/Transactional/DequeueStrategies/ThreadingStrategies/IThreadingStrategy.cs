namespace NServiceBus.Unicast.Transport.Transactional.DequeueStrategies.ThreadingStrategies
{
    using System;

    public interface IThreadingStrategy
    {
        void Start(int maximumConcurrencyLevel, Action workerMethod);
        void Stop();
    }
}