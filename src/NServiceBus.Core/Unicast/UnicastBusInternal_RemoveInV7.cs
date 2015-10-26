namespace NServiceBus.Unicast
{
    using System;

    partial class RunningEndpoint
    {
        public ICallback Defer(TimeSpan delay, object message)
        {
            throw new NotImplementedException();
        }

        public ICallback Defer(DateTime processAt, object message)
        {
            throw new NotImplementedException();
        }
 
        public void Return<T>(T errorEnum)
        {
            throw new NotImplementedException();
        }
    }
}