using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.Instances;
using NServiceBus.SagaPersisters.NHibernate.AutoPersistence.Attributes;

namespace NServiceBus.SagaPersisters.NHibernate.AutoPersistence.Conventions
{
    public class UniqueConvention : IPropertyConvention
    {
        public void Apply(IPropertyInstance instance)
        {
            foreach (object attribute in instance.Property.MemberInfo.GetCustomAttributes(true))
                if (attribute is UniqueAttribute)
                    instance.Unique();
        }
    }
}