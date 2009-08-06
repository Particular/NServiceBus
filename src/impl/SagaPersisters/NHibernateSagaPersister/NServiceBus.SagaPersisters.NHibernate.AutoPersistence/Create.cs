using System;
using System.Collections.Generic;
using System.Linq;
using FluentNHibernate.AutoMap;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.NHibernate.AutoPersistence.Conventions;

namespace NServiceBus.SagaPersisters.NHibernate.AutoPersistence
{
    public static class Create
    {
        public static AutoPersistenceModel SagaPersistenceModel(IEnumerable<Type> typesToScan)
        {
            var sagaEntites = typesToScan.Where(t => typeof(ISagaEntity).IsAssignableFrom(t) && !t.IsInterface);

            var assembliesContainingSagas = sagaEntites.Select(t => t.Assembly).Distinct();

            if (assembliesContainingSagas.Count() == 0)
                return null;

            var model = new AutoPersistenceModel();

            foreach (var assembly in assembliesContainingSagas)
                model.AddEntityAssembly(assembly);

            model.ConventionDiscovery.AddFromAssemblyOf<IdShouldBeAssignedConvention>()
                .Where(t => typeof(ISagaEntity).IsAssignableFrom(t) || t.GetProperty("Id") != null);
            return model;
        }
    }
}
