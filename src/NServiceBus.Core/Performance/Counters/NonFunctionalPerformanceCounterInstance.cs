namespace NServiceBus
{
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