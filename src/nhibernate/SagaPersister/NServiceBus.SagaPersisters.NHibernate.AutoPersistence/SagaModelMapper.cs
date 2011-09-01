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
    public ConventionModelMapper Mapper { get; private set; }
    private readonly IEnumerable<Type> _entityTypes;
    private readonly IEnumerable<Type> _sagaEntites;

    public SagaModelMapper(IEnumerable<Type> typesToScan)
    {
      Mapper = new ConventionModelMapper();

      _sagaEntites = typesToScan.Where(t => typeof(ISagaEntity).IsAssignableFrom(t) && !t.IsInterface);

      _entityTypes = GetTypesThatShouldBeAutoMapped(_sagaEntites, typesToScan);

      Mapper.BeforeMapClass += ApplyClassConvention;
      Mapper.BeforeMapJoinedSubclass += ApplySubClassConvention;
      Mapper.BeforeMapProperty += ApplyPropertyConvention;
      Mapper.BeforeMapBag += ApplyBagConvention;
      Mapper.BeforeMapManyToOne += ApplyManyToOneConvention;
    }

    private void ApplyClassConvention(IModelInspector mi, Type type, IClassAttributesMapper map)
    {
      if (!_sagaEntites.Contains(type))
        map.Id(idMapper => idMapper.Generator(Generators.GuidComb));
      else
        map.Id(idMapper => idMapper.Generator(Generators.Assigned));

      var tableAttribute = GetAttribute<TableNameAttribute>(type);
      if (tableAttribute != null)
      {
        map.Table(tableAttribute.TableName);
        if (!String.IsNullOrEmpty(tableAttribute.Schema))
          map.Schema(tableAttribute.Schema);
      }
    }

    private void ApplySubClassConvention(IModelInspector mi, Type type, IJoinedSubclassAttributesMapper map)
    {
      map.Key(keyMapping => keyMapping.Column(String.Format("{0}_id", type.BaseType.Name)));

      var tableAttribute = GetAttribute<TableNameAttribute>(type);
      if (tableAttribute != null)
      {
        map.Table(tableAttribute.TableName);
        if (!String.IsNullOrEmpty(tableAttribute.Schema))
          map.Schema(tableAttribute.Schema);
      }
    }

    private void ApplyPropertyConvention(IModelInspector mi, PropertyPath type, IPropertyMapper map)
    {
      if (type.PreviousPath != null)
        if (mi.IsComponent(((PropertyInfo)type.PreviousPath.LocalMember).PropertyType))
          map.Column(type.PreviousPath.LocalMember.Name + type.LocalMember.Name);

      if (type.LocalMember.GetCustomAttributes(typeof(UniqueAttribute), false).Any())
        map.Unique(true);
    }
    
    private void ApplyBagConvention(IModelInspector mi, PropertyPath type, IBagPropertiesMapper map)
    {
      map.Cascade(Cascade.All | Cascade.DeleteOrphans);
      map.Key(km => km.Column(type.LocalMember.DeclaringType.Name + "_id"));
    }

    private void ApplyManyToOneConvention(IModelInspector mi, PropertyPath type, IManyToOneMapper map)
    {
      map.Column(type.LocalMember.Name + "_id");
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
}