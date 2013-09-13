using System;
using NServiceBus;

namespace MyMessages
{
    public class TestStarterMessage : IMessage
    {
        public Guid Id { get; set; }
    }
}