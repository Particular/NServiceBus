using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.Instances;

namespace NServiceBus.SagaPersisters.NHibernate.AutoPersistence.Conventions
{
    public class CollectionsShouldCascadeConvention : IHasManyConvention
    {
        public void Apply(IOneToManyCollectionInstance instance)
        {
            instance.Cascade.AllDeleteOrphan();
        }
    }
}