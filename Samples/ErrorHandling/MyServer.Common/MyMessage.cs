using System;
using NServiceBus;

namespace MyServer.Common
{
    public class MyMessage : IMessage
    {
        public Guid Id { get; set; }
    }
}