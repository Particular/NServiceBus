namespace NServiceBus.Unicast.Transport.Transactional.DequeueStrategies.ThreadingStrategies
{
    using System;

    public interface IThreadingStrategy
    {
        void ChangeMaxDegreeOfParallelism(int value);
        void Start(int maxDegreeOfParallelism,Action workerMethod);
        void Stop();
    }
}