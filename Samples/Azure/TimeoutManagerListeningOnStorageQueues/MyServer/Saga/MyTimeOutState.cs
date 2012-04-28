namespace MyServer.Saga
{
    using NServiceBus.Saga;

    public class MyTimeOutState: ITimeoutState
    {
        public int SomeValue { get; set; }
    }
}