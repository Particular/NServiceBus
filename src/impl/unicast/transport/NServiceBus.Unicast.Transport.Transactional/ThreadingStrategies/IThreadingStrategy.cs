namespace NServiceBus.Unicast.Transport.Transactional.ThreadingStrategies
{
    using System;

    public interface IThreadingStrategy
    {
        void ChangeMaxDegreeOfParallelism(int value);
        void Start(int maxDegreeOfParallelism,Action workerMethod);
        void Stop();
    }
}