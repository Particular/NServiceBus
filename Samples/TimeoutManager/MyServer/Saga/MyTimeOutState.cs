namespace MyServer.Saga
{
    using NServiceBus.Saga;

    public class MyTimeOutState:ITimeoutState
    {
        public string SomeValue { get; set; }
    }
}