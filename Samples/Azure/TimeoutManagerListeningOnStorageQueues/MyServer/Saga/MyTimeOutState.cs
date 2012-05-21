namespace MyServer.Saga
{
    using NServiceBus.Saga;

    public class MyTimeOutState
    {
        public int SomeValue { get; set; }
    }
}