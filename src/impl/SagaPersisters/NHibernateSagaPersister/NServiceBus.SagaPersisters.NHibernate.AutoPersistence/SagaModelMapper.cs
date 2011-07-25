using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.NHibernate.AutoPersistence.Attributes;

namespace NServiceBus.SagaPersisters.NHibernate.AutoPersistence
{
  public class SagaModelMapper
  {
    private readonly IEnumerable<Type> _typesToScan;
    public ConventionModelMapper Mapper { get; private set; }
    private readonly IEnumerable<Type> _entityTypes;

    public SagaModelMapper(IEnumerable<Type> typesToScan)
    {
      _typesToScan = typesToScan;

      Mapper = new ConventionModelMapper();

      var sagaEntites = typesToScan.Where(t => typeof(ISagaEntity).IsAssignableFrom(t) && !t.IsInterface);

      _entityTypes = GetTypesThatShouldBeAutoMapped(sagaEntites, typesToScan);

      Mapper.BeforeMapBag += (mi, t, map) =>
      {
        map.Cascade(Cascade.All | Cascade.DeleteOrphans);
        map.Key(km => km.Column(t.LocalMember.DeclaringType.Name + "_id"));
      };

      Mapper.BeforeMapManyToOne += (mi, t, map) =>
      {
        map.Column(t.LocalMember.Name + "_id");
      };

      Mapper.BeforeMapClass += (mi, t, map) =>
      {
        if (!sagaEntites.Contains(t))
          map.Id(idMapper => idMapper.Generator(Generators.GuidComb));
        else
          map.Id(idMapper => idMapper.Generator(Generators.Assigned));

        var tableAttribute = GetAttribute<TableNameAttribute>(t);
        if (tableAttribute != null)
        {
          map.Table(tableAttribute.TableName);
          if (!String.IsNullOrEmpty(tableAttribute.Schema))
            map.Schema(tableAttribute.Schema);
        }
      };

      Mapper.BeforeMapJoinedSubclass += (mi, t, map) =>
      {
        map.Key(keyMapping => keyMapping.Column(String.Format("{0}_id", t.BaseType.Name)));

        var tableAttribute = GetAttribute<TableNameAttribute>(t);
        if (tableAttribute != null)
        {
          map.Table(tableAttribute.TableName);
          if (!String.IsNullOrEmpty(tableAttribute.Schema))
            map.Schema(tableAttribute.Schema);
        }
      };

      Mapper.BeforeMapProperty += (mi, t, map) =>
      {
        if (t.PreviousPath != null)
          if (mi.IsComponent(((PropertyInfo)t.PreviousPath.LocalMember).PropertyType))
            map.Column(t.PreviousPath.LocalMember.Name + t.LocalMember.Name);

        if (t.LocalMember.GetCustomAttributes(typeof(UniqueAttribute), false).Any())
          map.Unique(true);
      };
    }

    public HbmMapping Compile()
    {
      return Mapper.CompileMappingFor(_entityTypes);
    }

    private static IEnumerable<Type> GetTypesThatShouldBeAutoMapped(IEnumerable<Type> sagaEntites, IEnumerable<Type> typesToScan)
    {
      IList<Type> entityTypes = new List<Type>();

      foreach (var rootEntity in sagaEntites)
      {
        AddEntitesToBeMapped(rootEntity, entityTypes, typesToScan);
      }
      return entityTypes;
    }

    private static void AddEntitesToBeMapped(Type rootEntity, ICollection<Type> foundEntities, IEnumerable<Type> typesToScan)
    {
      if (foundEntities.Contains(rootEntity))
        return;

      foundEntities.Add(rootEntity);

      var propertyTypes = rootEntity.GetProperties()
             .Select(p => p.PropertyType);

      foreach (var propertyType in propertyTypes)
      {
        if (propertyType.GetProperty("Id") != null)
          AddEntitesToBeMapped(propertyType, foundEntities, typesToScan);

        if (propertyType.IsGenericType)
        {
          var args = propertyType.GetGenericArguments();

          if (args[0].GetProperty("Id") != null)
            AddEntitesToBeMapped(args[0], foundEntities, typesToScan);

        }
      }
      var derivedTypes = typesToScan.Where(t => t.IsSubclassOf(rootEntity));

      foreach (var derivedType in derivedTypes)
      {
        AddEntitesToBeMapped(derivedType, foundEntities, typesToScan);
      }
    }

    static T GetAttribute<T>(Type type) where T : Attribute
    {
      var attributes = type.GetCustomAttributes(typeof(T), false);
      return attributes.FirstOrDefault() as T;
    }
  }


  //public static class Create2
  //{
  //      public static HbmMapping SagaPersistenceModel(IEnumerable<Type> typesToScan)
  //      {
  //        var mapper = new ConventionModelMapper();

  //        var sagaEntites = typesToScan.Where(t => typeof(ISagaEntity).IsAssignableFrom(t) && !t.IsInterface);

  //        var entityTypes = GetTypesThatShouldBeAutoMapped(sagaEntites, typesToScan);
          
  //        mapper.BeforeMapBag += (mi, t, map) =>
  //        {
  //          map.Cascade(Cascade.All | Cascade.DeleteOrphans);
  //          map.Key(km => km.Column(t.LocalMember.DeclaringType.Name + "_id"));
  //        };

  //        mapper.BeforeMapManyToOne += (mi, t, map) =>
  //        {
  //          map.Column(t.LocalMember.Name + "_id");
  //        };

  //        mapper.BeforeMapClass += (mi, t, map) =>
  //                                   {
  //                                     if (!sagaEntites.Contains(t))
  //                                       map.Id(idMapper => idMapper.Generator(Generators.GuidComb));
  //                                     else
  //                                       map.Id(idMapper => idMapper.Generator(Generators.Assigned));

  //                                     var tableAttribute = GetAttribute<TableNameAttribute>(t);
  //                                     if (tableAttribute != null)
  //                                     {
  //                                       map.Table(tableAttribute.TableName);
  //                                       if (!String.IsNullOrEmpty(tableAttribute.Schema))
  //                                         map.Schema(tableAttribute.Schema);
  //                                     }
  //                                   };

  //        mapper.BeforeMapJoinedSubclass += (mi, t, map) =>
  //                                            {
  //                                              map.Key(keyMapping => keyMapping.Column(String.Format("{0}_id", t.BaseType.Name)));

  //                                              var tableAttribute = GetAttribute<TableNameAttribute>(t);
  //                                              if (tableAttribute != null)
  //                                              {
  //                                                map.Table(tableAttribute.TableName);
  //                                                if (!String.IsNullOrEmpty(tableAttribute.Schema))
  //                                                  map.Schema(tableAttribute.Schema);
  //                                              }
  //                                            };

  //        mapper.BeforeMapProperty += (mi, t, map) =>
  //                                     {
  //                                       if (t.PreviousPath != null)
  //                                         if (mi.IsComponent(((PropertyInfo)t.PreviousPath.LocalMember).PropertyType))
  //                                           map.Column(t.PreviousPath.LocalMember.Name + t.LocalMember.Name);

  //                                       if (t.LocalMember.GetCustomAttributes(typeof(UniqueAttribute), false).Any())
  //                                         map.Unique(true);
  //                                     };

  //        return mapper.CompileMappingFor(entityTypes);
  //      }
  //}
}