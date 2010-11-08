using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.Instances;
using NServiceBus.Saga;

namespace NServiceBus.SagaPersisters.NHibernate.AutoPersistence.Conventions
{
    public class IdShouldBeAssignedConvention : IIdConvention 

    {
        public void Apply(IIdentityInstance instance)
        {
            if (typeof(ISagaEntity).IsAssignableFrom(instance.EntityType))
            {
                instance.GeneratedBy.Assigned();
            }
            else
                instance.GeneratedBy.GuidComb();
        }


   
    }
}