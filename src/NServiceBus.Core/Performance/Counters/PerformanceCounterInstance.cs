namespace NServiceBus
{
    using System.Diagnostics;

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
}