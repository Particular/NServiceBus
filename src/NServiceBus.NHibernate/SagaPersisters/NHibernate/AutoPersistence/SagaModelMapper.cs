namespace NServiceBus.SagaPersisters.NHibernate.AutoPersistence
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Attributes;
    using Saga;
    using global::NHibernate.Cfg.MappingSchema;
    using global::NHibernate.Mapping.ByCode;

    public class SagaModelMapper
    {
        public ConventionModelMapper Mapper { get; private set; }
        readonly IEnumerable<Type> _entityTypes;
        readonly IEnumerable<Type> _sagaEntites;

        public SagaModelMapper(IEnumerable<Type> typesToScan)
        {
            Mapper = new ConventionModelMapper();

            _sagaEntites =
                typesToScan.Where(t => typeof (IContainSagaData).IsAssignableFrom(t) && !t.IsInterface);

            _entityTypes = GetTypesThatShouldBeAutoMapped(_sagaEntites, typesToScan);

            Mapper.IsTablePerClass((type, b) => false);
            Mapper.IsTablePerConcreteClass((type, b) => _sagaEntites.Contains(type));
            Mapper.IsTablePerClassHierarchy((type, b) => false);
            Mapper.IsEntity((type, mapped) => _entityTypes.Contains(type));
            Mapper.IsArray((info, b) => false);
            Mapper.IsBag((info, b) =>
                {
                    var memberType = info.GetPropertyOrFieldType();
                    return typeof (IEnumerable).IsAssignableFrom(memberType) &&
                           !(memberType == typeof (string) || memberType == typeof (byte[]) || memberType.IsArray);
                });
            Mapper.IsPersistentProperty((info, b) => !HasAttribute<RowVersionAttribute>(info));

            Mapper.BeforeMapClass += ApplyClassConvention;
            Mapper.BeforeMapUnionSubclass += ApplySubClassConvention;
            Mapper.BeforeMapProperty += ApplyPropertyConvention;
            Mapper.BeforeMapBag += ApplyBagConvention;
            Mapper.BeforeMapManyToOne += ApplyManyToOneConvention;
        }

        void ApplyClassConvention(IModelInspector mi, Type type, IClassAttributesMapper map)
        {
            if (!_sagaEntites.Contains(type))
                map.Id(idMapper => idMapper.Generator(Generators.GuidComb));
            else
                map.Id(idMapper => idMapper.Generator(Generators.Assigned));

            var tableAttribute = GetAttribute<TableNameAttribute>(type);

            var rowVersionProperty = type.GetProperties()
                                         .Where(HasAttribute<RowVersionAttribute>)
                                         .FirstOrDefault();

            if (rowVersionProperty != null)
                map.Version(rowVersionProperty, mapper => mapper.Generated(VersionGeneration.Always));

            if (tableAttribute != null)
            {
                map.Table(tableAttribute.TableName);
                if (!String.IsNullOrEmpty(tableAttribute.Schema))
                    map.Schema(tableAttribute.Schema);

                return;
            }

            //if the type is nested use the name of the parent
            if (type.DeclaringType != null)
            {
                if (typeof (IContainSagaData).IsAssignableFrom(type))
                {
                    map.Table(type.DeclaringType.Name);
                }
                else
                {
                    map.Table(type.DeclaringType.Name + "_" + type.Name);
                }
            }
        }

        void ApplySubClassConvention(IModelInspector mi, Type type, IUnionSubclassAttributesMapper map)
        {
            var tableAttribute = GetAttribute<TableNameAttribute>(type);
            if (tableAttribute != null)
            {
                map.Table(tableAttribute.TableName);
                if (!String.IsNullOrEmpty(tableAttribute.Schema))
                    map.Schema(tableAttribute.Schema);

                return;
            }

            //if the type is nested use the name of the parent
            if (type.DeclaringType != null)
            {
                if (typeof (IContainSagaData).IsAssignableFrom(type))
                {
                    map.Table(type.DeclaringType.Name);
                }
                else
                {
                    map.Table(type.DeclaringType.Name + "_" + type.Name);
                }
            }
        }

        void ApplyPropertyConvention(IModelInspector mi, PropertyPath type, IPropertyMapper map)
        {
            if (type.PreviousPath != null)
                if (mi.IsComponent(((PropertyInfo) type.PreviousPath.LocalMember).PropertyType))
                    map.Column(type.PreviousPath.LocalMember.Name + type.LocalMember.Name);

            if (type.LocalMember.GetCustomAttributes(typeof (UniqueAttribute), false).Any())
                map.Unique(true);
        }

        void ApplyBagConvention(IModelInspector mi, PropertyPath type, IBagPropertiesMapper map)
        {
            map.Cascade(Cascade.All | Cascade.DeleteOrphans);
            map.Key(km => km.Column(type.LocalMember.DeclaringType.Name + "_id"));
        }

        void ApplyManyToOneConvention(IModelInspector mi, PropertyPath type, IManyToOneMapper map)
        {
            map.Column(type.LocalMember.Name + "_id");
        }

        public HbmMapping Compile()
        {
            var hbmMapping = Mapper.CompileMappingFor(_entityTypes);

            ApplyOptimisticLockingOnMapping(hbmMapping);

            return hbmMapping;
        }

        static void ApplyOptimisticLockingOnMapping(HbmMapping hbmMapping)
        {
            foreach (var rootClass in hbmMapping.RootClasses)
            {
                if (rootClass.Version != null)
                    continue;

                rootClass.dynamicupdate = true;
                rootClass.optimisticlock = HbmOptimisticLockMode.All;
            }

            foreach (var hbmSubclass in hbmMapping.UnionSubclasses)
                hbmSubclass.dynamicupdate = true;
            foreach (var hbmSubclass in hbmMapping.JoinedSubclasses)
                hbmSubclass.dynamicupdate = true;
            foreach (var hbmSubclass in hbmMapping.SubClasses)
                hbmSubclass.dynamicupdate = true;
        }

        static IEnumerable<Type> GetTypesThatShouldBeAutoMapped(IEnumerable<Type> sagaEntites,
                                                                IEnumerable<Type> typesToScan)
        {
            IList<Type> entityTypes = new List<Type>();

            foreach (var rootEntity in sagaEntites)
            {
                AddEntitiesToBeMapped(rootEntity, entityTypes, typesToScan);
            }

            return entityTypes;
        }

        static void AddEntitiesToBeMapped(Type rootEntity, ICollection<Type> foundEntities,
                                         IEnumerable<Type> typesToScan)
        {
            if (foundEntities.Contains(rootEntity))
                return;

            foundEntities.Add(rootEntity);

            var propertyTypes = rootEntity.GetProperties()
                                          .Select(p => p.PropertyType);

            foreach (var propertyType in propertyTypes)
            {
                if (propertyType.GetProperty("Id") != null)
                    AddEntitiesToBeMapped(propertyType, foundEntities, typesToScan);

                if (propertyType.IsGenericType)
                {
                    var args = propertyType.GetGenericArguments();

                    if (args[0].GetProperty("Id") != null)
                        AddEntitiesToBeMapped(args[0], foundEntities, typesToScan);

                }
            }
            var derivedTypes = typesToScan.Where(t => t.IsSubclassOf(rootEntity));

            foreach (var derivedType in derivedTypes)
            {
                AddEntitiesToBeMapped(derivedType, foundEntities, typesToScan);
            }

            var superClasses = typesToScan.Where(t => t.IsAssignableFrom(rootEntity));

            foreach (var superClass in superClasses)
            {
                AddEntitiesToBeMapped(superClass, foundEntities, typesToScan);
            }

        }

        static T GetAttribute<T>(Type type) where T : Attribute
        {
            var attributes = type.GetCustomAttributes(typeof (T), false);
            return attributes.FirstOrDefault() as T;
        }

        static bool HasAttribute<T>(MemberInfo mi) where T : Attribute
        {
            var attributes = mi.GetCustomAttributes(typeof (T), false);
            return attributes.Any();
        }
    }
}