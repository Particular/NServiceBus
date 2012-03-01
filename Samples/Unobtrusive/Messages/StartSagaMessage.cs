using System;

namespace Messages
{
    public class StartSagaMessage
    {
        public Guid OrderId { get; set; }
    }
}