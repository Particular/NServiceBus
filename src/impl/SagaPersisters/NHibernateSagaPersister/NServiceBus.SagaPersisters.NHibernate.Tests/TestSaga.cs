using System;
using System.Collections.Generic;
using NServiceBus.Saga;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    public class TestSaga:ISagaEntity
    {
        public virtual Guid Id{ get; set;}

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        public virtual RelatedClass RelatedClass { get; set; }

        public virtual IList<OrderLine> OrderLines { get; set; }

        public virtual StatusEnum Status { get; set; }
    }

    public enum StatusEnum
    {
        SomeStatus, AnotherStatus
    }

    public class OrderLine
    {
        public virtual Guid Id { get; set; }

        public virtual Guid ProductId { get; set; }

    }


    public class RelatedClass
    {
        public virtual Guid Id { get; set; }

    }

}