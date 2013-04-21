namespace MyServer.Saga
{
    using System;
    using NServiceBus;

    public class StartSagaMessage: ICommand
    {
        public Guid OrderId { get; set; }
    }
}