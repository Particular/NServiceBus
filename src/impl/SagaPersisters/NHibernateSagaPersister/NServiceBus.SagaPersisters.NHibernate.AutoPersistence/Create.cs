using System;
using System.Collections.Generic;
using System.Linq;
using FluentNHibernate.Automapping;
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

            model.Conventions.AddFromAssemblyOf<IdShouldBeAssignedConvention>();

            foreach (var assembly in assembliesContainingSagas)
                model.AddEntityAssembly(assembly)
               .Where(t => typeof(ISagaEntity).IsAssignableFrom(t) || t.GetProperty("Id") != null);

            
            
            model.BuildMappings();
            return model;
        }
    }
}
