using FluentNHibernate.Conventions;
using FluentNHibernate.Mapping;
using NServiceBus.Saga;

namespace NServiceBus.SagaPersisters.NHibernate.AutoPersistence.Conventions
{
    public class IdShouldBeAssignedConvention : IIdConvention
    {
        public bool Accept(IIdentityPart target)
        {
            return typeof(ISagaEntity).IsAssignableFrom(target.EntityType);
        }

        public void Apply(IIdentityPart target)
        {
            target.GeneratedBy.Assigned();
        }
    }
}