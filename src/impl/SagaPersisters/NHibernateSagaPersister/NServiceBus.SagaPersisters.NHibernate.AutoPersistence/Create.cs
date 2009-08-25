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

            var model = new AutoPersistenceModel();

            model.Conventions.AddFromAssemblyOf<IdShouldBeAssignedConvention>();

            var componentTypes = GetTypesThatShouldBeMappedAsComponents(sagaEntites);

            foreach (var assembly in assembliesContainingSagas)
                model. AddEntityAssembly(assembly)
               .Where(t =>
                   typeof(ISagaEntity).IsAssignableFrom(t) ||
                   t.GetProperty("Id") != null ||
                   componentTypes.Contains(t));

            model.Setup(s =>
                          {
                              s.IsComponentType =
                                  type => componentTypes.Contains(type);
                          });

            return model;
        }

        private static IEnumerable<Type> GetTypesThatShouldBeMappedAsComponents(IEnumerable<Type> sagaEntites)
        {
            IEnumerable<Type> componentTypes = new List<Type>();

            foreach (var sagaEntity in sagaEntites)
            {
                var propertyTypes = sagaEntity.GetProperties()
                    .Select(p => p.PropertyType)
                    .Where(t =>
                           !t.Namespace.StartsWith("System") &&
                           t.GetProperty("Id") == null &&
                           !t.IsEnum);

                componentTypes = componentTypes.Concat(propertyTypes);
            }
            return componentTypes;
        }
    }
}
