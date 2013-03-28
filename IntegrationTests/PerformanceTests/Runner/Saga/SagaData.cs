﻿namespace Runner.Saga
{
    using System;

    using NServiceBus.Saga;

    public class SagaData : ISagaEntity
    {
        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        public virtual Guid Id { get; set; }

        [Unique]
        public virtual int Number { get; set; }
    }
}