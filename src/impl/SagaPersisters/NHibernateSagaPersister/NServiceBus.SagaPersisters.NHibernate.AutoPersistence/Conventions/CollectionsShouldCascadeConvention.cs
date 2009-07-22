using FluentNHibernate.Conventions;
using FluentNHibernate.Mapping;

namespace NServiceBus.SagaPersisters.NHibernate.AutoPersistence.Conventions
{
    public class CollectionsShouldCascadeConvention : IHasManyConvention
    {
        public bool Accept(IOneToManyPart target)
        {
            return true;
        }

        public void Apply(IOneToManyPart target)
        {
            target.Cascade.AllDeleteOrphan();
        }
    }
}