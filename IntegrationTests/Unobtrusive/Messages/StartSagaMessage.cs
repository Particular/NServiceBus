namespace Messages
{
    using System;

    public class StartSagaMessage
    {
        public Guid OrderId { get; set; }
    }
}