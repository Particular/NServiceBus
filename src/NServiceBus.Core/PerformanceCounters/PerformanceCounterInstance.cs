namespace NServiceBus
{
    using System.Diagnostics;

    class PerformanceCounterInstance : IPerformanceCounterInstance
    {
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

        PerformanceCounter counter;
    }
}