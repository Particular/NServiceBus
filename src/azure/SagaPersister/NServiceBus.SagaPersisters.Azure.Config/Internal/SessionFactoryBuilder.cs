using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using NHibernate;
using NHibernate.Context;
using NHibernate.Drivers.Azure.TableStorage.Mapping;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using NServiceBus.SagaPersisters.NHibernate.AutoPersistence;
using Configuration=NHibernate.Cfg.Configuration;

namespace NServiceBus.SagaPersisters.Azure.Config.Internal
{
    /// <summary>
    /// Builder class for the NHibernate Session Factory
    /// </summary>
    public class SessionFactoryBuilder
    {
        private readonly IEnumerable<Type> typesToScan;

        /// <summary>
        /// Constructor that accepts the types to scan for saga data classes
        /// </summary>
        /// <param name="typesToScan"></param>
        public SessionFactoryBuilder(IEnumerable<Type> typesToScan)
        {
            this.typesToScan = typesToScan;
        }

        /// <summary>
        /// Builds the session factory with the given properties. Database is updated if updateSchema is set
        /// </summary>
        /// <param name="nhibernateProperties"></param>
        /// <param name="updateSchema"></param>
        /// <returns></returns>
        public ISessionFactory Build(IDictionary<string, string> nhibernateProperties, bool updateSchema)
        {
          var scannedAssemblies = typesToScan.Select(t => t.Assembly).Distinct();

          var nhibernateConfiguration = new Configuration().SetProperties(nhibernateProperties);

          foreach (var assembly in scannedAssemblies)
            nhibernateConfiguration.AddAssembly(assembly);

          var mapping = new SagaModelMapper(typesToScan.Except(nhibernateConfiguration.ClassMappings.Select(x => x.MappedClass)));

          HackIdIntoMapping(mapping);

          nhibernateConfiguration.AddMapping(mapping.Compile());

          ApplyDefaultsTo(nhibernateConfiguration);

          if (updateSchema)
            UpdateDatabaseSchemaUsing(nhibernateConfiguration);

          try
          {
            return nhibernateConfiguration.BuildSessionFactory();
          }
          catch (Exception e)
          {
            if (e.InnerException != null)
              throw new ConfigurationErrorsException(e.InnerException.Message, e);

            throw;
          }
        }

        private static void HackIdIntoMapping(SagaModelMapper hbmMapping)
        {
          var hbmIdField = typeof(global::NHibernate.Mapping.ByCode.Impl.IdMapper).GetField("hbmId", BindingFlags.Instance | BindingFlags.NonPublic);

          hbmMapping.Mapper.AfterMapClass += (mi, t, map) =>
          {
            map.Id(idmap =>
            {
              var hbmId = (global::NHibernate.Cfg.MappingSchema.HbmId)hbmIdField.GetValue(idmap);
              hbmId.type1 = typeof(GuidToPartitionKeyAndRowKey).AssemblyQualifiedName;
              hbmId.type = null;
              hbmId.column1 = null;
              hbmId.column = new[]
                           {
                             new global::NHibernate.Cfg.MappingSchema.HbmColumn {name = "RowKey"},
                             new global::NHibernate.Cfg.MappingSchema.HbmColumn {name = "PartitionKey"},
                           };
            });
          };

          hbmMapping.Mapper.AfterMapManyToOne += (mi, type, map) => MapIdColumns(map, type.LocalMember);
          hbmMapping.Mapper.AfterMapBag += (mi, type, map) => map.Key(km => MapIdColumns(km, type.LocalMember));
          hbmMapping.Mapper.AfterMapJoinedSubclass += (mi, type, map) => map.Key(km => MapIdColumns(km, type.BaseType));
        }

        private static void MapIdColumns(IColumnsMapper map, MemberInfo type)
        {
          map.Columns(cm => cm.Name(type.Name + "_RowKey"),
                      cm => cm.Name(type.Name + "_PartitionKey"));
        }

        private static void UpdateDatabaseSchemaUsing(Configuration configuration)
        {
          new SchemaUpdate(configuration)
              .Execute(false, true);
        }

        private static void ApplyDefaultsTo(Configuration configuration)
        {

          configuration.SetProperty("current_session_context_class", typeof(ThreadStaticSessionContext).AssemblyQualifiedName);
        }
    }
}