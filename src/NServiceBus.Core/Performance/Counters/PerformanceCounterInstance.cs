namespace NServiceBus.Performance.Counters
{
    using System;
    using System.Diagnostics;

    interface IPerformanceCounterInstance : IDisposable
    {
        void Increment();
    }

    class PerformanceCounterInstance : IPerformanceCounterInstance
    {
        PerformanceCounter counter;

        public PerformanceCounterInstance(PerformanceCounter counter)
        {
            this.counter = counter;
        }

        public void Increment()
        {
            counter.Increment();
        }

        public void Dispose()
        {
            //Injected via Fody
        }
    }

    class NonFunctionalPerformanceCounterInstance : IPerformanceCounterInstance
    {
        public void Increment()
        {
            //NOOP
        }

        public void Dispose()
        {
            //NOOP
        }
    }
}