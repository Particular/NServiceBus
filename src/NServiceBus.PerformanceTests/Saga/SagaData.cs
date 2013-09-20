namespace Runner.Saga
{
    using System;
    using NServiceBus.Saga;
    using NServiceBus.SagaPersisters.NHibernate;

    [LockMode(LockModes.None)] //this is the default but still
    public class SagaData : IContainSagaData
    {
        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        public virtual Guid Id { get; set; }

        [Unique]
        public virtual int Number { get; set; }

        public virtual int NumCalls { get; set; }
    }
}