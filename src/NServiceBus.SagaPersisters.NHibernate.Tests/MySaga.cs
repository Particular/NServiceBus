namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using Saga;

    public class MySaga : Saga<MySagaData>
    {
        [Obsolete("2.6 style timeouts has been replaced. Please implement IHandleTimeouts<T> instead. Will be removed in version \'5.0\'.", true)]
        public override void Timeout(object state)
        {
        }
    }

    public class MySagaData : IContainSagaData
    {
        public virtual Guid Id { get; set; }
        public virtual string OriginalMessageId { get; set; }
        public virtual string Originator { get; set; }
    }
}