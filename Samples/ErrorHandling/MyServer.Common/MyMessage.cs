namespace MyServer.Common
{
    using System;
    using NServiceBus;

    public class MyMessage : IMessage
    {
        public Guid Id { get; set; }
    }
}