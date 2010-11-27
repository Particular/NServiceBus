using System;
using System.Linq;
using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.AcceptanceCriteria;
using FluentNHibernate.Conventions.Inspections;
using FluentNHibernate.Conventions.Instances;
using NServiceBus.SagaPersisters.NHibernate.AutoPersistence.Attributes;

namespace NServiceBus.SagaPersisters.NHibernate.AutoPersistence.Conventions
{
    public class TableNameBasedOnAttributeConvention : IClassConvention, IJoinedSubclassConvention, IConventionAcceptance<IClassInspector>
    {
        public void Accept(IAcceptanceCriteria<IClassInspector> criteria)
        {
            criteria.Expect(x => GetAttribute(x.EntityType) != null);
        }

        public void Apply(IClassInstance instance)
        {
            var attribute = GetAttribute(instance.EntityType);
            if (attribute == null) return;
            instance.Table(attribute.TableName);
            if (!String.IsNullOrEmpty(attribute.Schema))
            {
                instance.Schema(attribute.Schema);
            }
        }

        public void Apply(IJoinedSubclassInstance instance)
        {
            var attribute = GetAttribute(instance.EntityType);
            if (attribute == null) return;
            instance.Table(attribute.TableName);
            if (!String.IsNullOrEmpty(attribute.Schema))
            {
                instance.Schema(attribute.Schema);
            }
        }

        static TableNameAttribute GetAttribute(Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(TableNameAttribute), false);
            return attributes.FirstOrDefault() as TableNameAttribute;
        }
    }
}