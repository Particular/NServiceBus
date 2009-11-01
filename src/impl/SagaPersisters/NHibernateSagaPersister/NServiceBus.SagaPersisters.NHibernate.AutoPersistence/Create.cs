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

            
            var model = new AutoPersistenceModel();

            model.Conventions.AddFromAssemblyOf<IdShouldBeAssignedConvention>();

            var entityTypes = GetTypesThatShouldBeAutoMapped(sagaEntites);

            var assembliesContainingSagas = sagaEntites.Select(t => t.Assembly).Distinct();
            
            foreach (var assembly in assembliesContainingSagas)
                model.AddEntityAssembly(assembly)
                    .Where(t => entityTypes.Contains(t));

            var componentTypes = GetTypesThatShouldBeMappedAsComponents(sagaEntites);

            model.Setup(s =>
                          {
                              s.IsComponentType =
                                  type => componentTypes.Contains(type);
                          });
            return model;
        }

        private static IEnumerable<Type> GetTypesThatShouldBeAutoMapped(IEnumerable<Type> sagaEntites)
        {
            IList<Type> entityTypes = new List<Type>();

            foreach (var rootEntity in sagaEntites)
            {
                AddEntitesToBeMapped(rootEntity,entityTypes);
            }
            return entityTypes;
        }

        private static void AddEntitesToBeMapped(Type rootEntity,ICollection<Type> foundEntities)
        {
            if(foundEntities.Contains(rootEntity))
                return;

            foundEntities.Add(rootEntity);

             var propertyTypes = rootEntity.GetProperties()
                    .Select(p => p.PropertyType);

            foreach (var propertyType in propertyTypes)
            {
                if (propertyType.GetProperty("Id") != null)
                    AddEntitesToBeMapped(propertyType, foundEntities);

                if (propertyType.IsGenericType)
                {
                    var args = propertyType.GetGenericArguments();

                    if(args[0].GetProperty("Id") != null)
                        AddEntitesToBeMapped(args[0],foundEntities);

                }
            }
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
