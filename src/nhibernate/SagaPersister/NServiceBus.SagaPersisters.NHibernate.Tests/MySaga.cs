using System;
using NServiceBus.Saga;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    public class MySaga : Saga<MySagaData>
    {
        public override void Timeout(object state)
        {
        }
    }

    public class MySagaData : ISagaEntity
    {
        public virtual Guid Id { get; set; }
        public virtual string OriginalMessageId { get; set; }
        public virtual string Originator { get; set; }
    }
}