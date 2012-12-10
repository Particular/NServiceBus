namespace MyServer.Saga
{
    using System;
    using NServiceBus.Saga;

    public class SimpleSagaData : ISagaEntity
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        [Unique]
        public Guid OrderId { get; set; }
    }
}