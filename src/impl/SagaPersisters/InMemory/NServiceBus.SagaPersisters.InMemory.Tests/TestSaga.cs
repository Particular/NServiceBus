﻿using System;
using System.Collections.Generic;
using NServiceBus.Saga;

namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    [Serializable]
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

        public override bool Equals(object obj)
        {
            return this.EqualTo(obj, (x, y) => x.Id == y.Id);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    [Serializable]
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

    [Serializable]
    public class PolymorpicProperty : PolymorpicPropertyBase
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

    [Serializable]
    public class PolymorpicPropertyBase
    {
        public virtual Guid Id { get; set; }
    }

    [Serializable]
    public enum StatusEnum
    {
        SomeStatus, AnotherStatus
    }

    [Serializable]
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

    [Serializable]
    public class OrderLine
    {
        public virtual Guid Id { get; set; }

        public virtual Guid ProductId { get; set; }

    }

    [Serializable]
    public class RelatedClass
    {
        public virtual Guid Id { get; set; }

        public virtual TestSaga ParentSaga { get; set; }
    }
}