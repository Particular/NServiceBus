using NServiceBus.Saga;

namespace Server.Saga
{
    public class MyTimeOutState : ITimeoutState
    {
        public int SomeValue { get; set; }
    }
}