namespace NServiceBus.SagaPersisters.NHibernate.AutoPersistence
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Serialization;
    using Attributes;
    using Saga;
    using global::NHibernate.Cfg.MappingSchema;
    using global::NHibernate.Mapping.ByCode;

    public class SagaModelMapper
	{
		public ConventionModelMapper Mapper { get; private set; }
		private readonly IEnumerable<Type> _entityTypes;
		private readonly IEnumerable<Type> _sagaEntites;

		public SagaModelMapper(IEnumerable<Type> typesToScan)
		{
			Mapper = new ConventionModelMapper();

			_sagaEntites = typesToScan.Where(t => typeof (ISagaEntity).IsAssignableFrom(t) && !t.IsInterface);

			_entityTypes = GetTypesThatShouldBeAutoMapped(_sagaEntites, typesToScan);

			Mapper.IsEntity((type, b) => _entityTypes.Contains(type));
			Mapper.IsArray((info, b) => false);
			Mapper.IsBag((info, b) =>
				{
					var memberType = info.GetPropertyOrFieldType();
					return typeof (IEnumerable).IsAssignableFrom(memberType) &&
						   !(memberType == typeof (string) || memberType == typeof (byte[]) || memberType.IsArray);
				});

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
				if (mi.IsComponent(((PropertyInfo) type.PreviousPath.LocalMember).PropertyType))
					map.Column(type.PreviousPath.LocalMember.Name + type.LocalMember.Name);

			if (type.LocalMember.GetCustomAttributes(typeof (UniqueAttribute), false).Any())
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

		public Stream Compile()
		{
            var hbmMapping = Mapper.CompileMappingFor(_entityTypes);
			
			var setting = new XmlWriterSettings { Indent = true };
			var serializer = new XmlSerializer(typeof(HbmMapping));
			using (var memStream = new MemoryStream(2048))
			{
				using (var xmlWriter = XmlWriter.Create(memStream, setting))
				{
					serializer.Serialize(xmlWriter, hbmMapping);
				}
				memStream.Position = 0;

				var xmlDoc = new XmlDocument();
				xmlDoc.Load(memStream);

				var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
				nsmgr.AddNamespace("nh", xmlDoc.DocumentElement.NamespaceURI);

                var classNodes = xmlDoc.DocumentElement.SelectNodes(@"/nh:hibernate-mapping/nh:class[not(nh:joined-subclass) and not(@name = /nh:hibernate-mapping/nh:joined-subclass/@extends)]", nsmgr);

				if (classNodes != null)
				{
					foreach (XmlElement classNode in classNodes)
					{
						var optimisticLockAttribute = xmlDoc.CreateAttribute("optimistic-lock");
						optimisticLockAttribute.Value = "all";
						classNode.Attributes.Append(optimisticLockAttribute);

						var dynamicUpdateAttribute = xmlDoc.CreateAttribute("dynamic-update");
						dynamicUpdateAttribute.Value = "true";
						classNode.Attributes.Append(dynamicUpdateAttribute);
					}
				}

				var memStreamOut = new MemoryStream(2048);
                xmlDoc.Save(memStreamOut);
                
				memStreamOut.Position = 0;

                return memStreamOut;
			}
		}

		private static IEnumerable<Type> GetTypesThatShouldBeAutoMapped(IEnumerable<Type> sagaEntites,
																		IEnumerable<Type> typesToScan)
		{
			IList<Type> entityTypes = new List<Type>();

			foreach (var rootEntity in sagaEntites)
			{
				AddEntitesToBeMapped(rootEntity, entityTypes, typesToScan);
			}
			return entityTypes;
		}

		private static void AddEntitesToBeMapped(Type rootEntity, ICollection<Type> foundEntities,
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

		private static T GetAttribute<T>(Type type) where T : Attribute
		{
			var attributes = type.GetCustomAttributes(typeof (T), false);
			return attributes.FirstOrDefault() as T;
		}
	}
}