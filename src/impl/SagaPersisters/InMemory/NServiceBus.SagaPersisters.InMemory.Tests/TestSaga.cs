namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Collections.Generic;
    using Saga;

    public class TestSaga : IContainSagaData
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        public virtual RelatedClass RelatedClass { get; set; }

        public virtual IList<OrderLine> OrderLines { get; set; }

        public virtual StatusEnum Status { get; set; }

        public virtual DateTime DateTimeProperty { get; set; }

        public virtual TestComponent TestComponent { get; set; }

        public virtual PolymorphicPropertyBase PolymorphicRelatedProperty { get; set; }

        public override bool Equals(object obj)
        {
            return this.EqualTo(obj, (x, y) => x.Id == y.Id);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public static class EqualityExtensions
    {
        public static bool EqualTo<T>(this T item, object obj, Func<T, T, bool> equals)
        {
            if (!(obj is T)) return false;

            var x = (T)obj;

            if (item != null && x == null) return false;

            if (item == null && x != null) return false;

            if (item == null && x == null) return true;

            return equals(item, x);
        }
    }
    
    public class PolymorphicProperty : PolymorphicPropertyBase
    {
        public virtual int SomeInt { get; set; }

        public override bool Equals(object obj)
        {
            return this.EqualTo(obj, (x, y) => x.Id == y.Id && x.SomeInt == y.SomeInt);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class PolymorphicPropertyBase
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

        public override bool Equals(object obj)
        {
            return this.EqualTo(obj, (x, y) =>
                x.Property == y.Property &&
                x.AnotherProperty == y.AnotherProperty);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
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
}