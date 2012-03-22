using System;
using System.Collections.Generic;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.NHibernate.AutoPersistence.Attributes;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    public class TestSaga : ISagaEntity
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        public virtual RelatedClass RelatedClass { get; set; }

        public virtual IList<OrderLine> OrderLines { get; set; }

        public virtual StatusEnum Status { get; set; }

        public virtual DateTime DateTimeProperty { get; set; }

        public virtual TestComponent TestComponent { get; set; }

        public virtual PolymorpicPropertyBase PolymorpicRelatedProperty { get; set; }

        public virtual int[] ArrayOfInts { get; set; }
        public virtual string[] ArrayOfStrings { get; set; }
        public virtual DateTime[] ArrayOfDates { get; set; }
    }

    public class PolymorpicProperty : PolymorpicPropertyBase
    {
        public virtual int SomeInt { get; set; }
    }

    public class PolymorpicPropertyBase
    {
        public virtual Guid Id { get; set; }
    }

    public enum StatusEnum
    {
        SomeStatus, AnotherStatus
    }

    public class TestComponent
    {
        public string Property { get; set; }
        public string AnotherProperty { get; set; }
    }

    public class OrderLine
    {
        public virtual Guid Id { get; set; }

        public virtual Guid ProductId { get; set; }

    }


    public class RelatedClass
    {
        public virtual Guid Id { get; set; }


        public virtual TestSaga ParentSaga { get; set; }
    }

    public class TestSagaWithHbmlXmlOverride : ISagaEntity
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        public virtual string SomeProperty { get; set; }
    }

    [TableName("MyTestTable", Schema = "MyTestSchema" )]
    public class TestSagaWithTableNameAttribute : ISagaEntity
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        public virtual string SomeProperty { get; set; }
    }

    public class DerivedFromTestSagaWithTableNameAttribute : TestSagaWithTableNameAttribute
    { }

    [TableName("MyDerivedTestTable")]
    public class AlsoDerivedFromTestSagaWithTableNameAttribute : TestSagaWithTableNameAttribute
    { }
}