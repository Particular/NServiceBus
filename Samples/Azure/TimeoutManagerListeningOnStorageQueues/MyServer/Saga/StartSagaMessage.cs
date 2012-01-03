namespace MyServer.Saga
{
    using System;
    using NServiceBus;

    public class StartSagaMessage:IMessage
    {
        public Guid OrderId { get; set; }
    }
}