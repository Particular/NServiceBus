namespace NServiceBus
{
    using System;

    interface IPerformanceCounterInstance : IDisposable
    {
        void Increment();
    }
}