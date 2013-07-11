namespace MyServer.Common
{
    using System;
    using NServiceBus;

    public class MyMessage : ICommand
    {
        public Guid Id { get; set; }
    }
}