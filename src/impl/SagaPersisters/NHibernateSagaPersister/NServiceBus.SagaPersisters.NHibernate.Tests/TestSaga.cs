using System;
using NServiceBus.Saga;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    public class TestSaga:ISagaEntity
    {
        public virtual Guid Id{ get; set;}

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        public virtual RelatedClass RelatedClass { get; set; }
    }


    public class RelatedClass
    {
        public virtual Guid Id { get; set; }

    }

}