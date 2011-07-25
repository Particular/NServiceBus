using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Dialect;
using NHibernate.Mapping.ByCode;
using NServiceBus.Faults.NHibernate;
using NServiceBus.ObjectBuilder;
using NHibernate.Cfg;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using Environment=NHibernate.Cfg.Environment;

namespace NServiceBus
{
   /// <summary>
   /// Configures fault handling using database/NHibernate fault manager.
   /// </summary>
   public static class ConfigureNHibernateFaultManager
   {
      /// <summary>
      /// Configures NServiceBus to persists fault information in database using default NHibernate configuration.
      /// </summary>
      /// <param name="config"></param>
      /// <returns></returns>
      public static Configure NHibernateFaultManager(this Configure config)
      {
         return NHibernateFaultManager(config, false);
      }

      /// <summary>
      /// Configures NServiceBus to persists fault information in database using default NHibernate configuration.      
      /// </summary>
      /// <param name="config"></param>
      /// <param name="autoUpdateSchema">Should fault information tables be re-created at start time?</param>
      /// <returns></returns>
      public static Configure NHibernateFaultManager(this Configure config, bool autoUpdateSchema)
      {
         config.Configurer.RegisterSingleton<FaultManagerSessionFactory>(
            CreateSessionFactory(new Configuration().Configure(), autoUpdateSchema));

         config.Configurer.ConfigureComponent<FaultManager>(DependencyLifecycle.SingleInstance);            
         return config;
      }

      /// <summary>
      /// Configures NServiceBus to persists fault information in database using named NHibernate configuration section.
      /// Provided section should be of type <see cref="NHibernate.Cfg.ConfigurationSectionHandler"/>.
      /// </summary>
      /// <param name="config"></param>
      /// <param name="hibernateSectionName">Name of NHibernate configuration section to use.</param>
      /// <returns></returns>
      public static Configure NHibernateFaultManager(this Configure config, string hibernateSectionName)
      {
         return NHibernateFaultManager(config, hibernateSectionName, false);
      }

      /// <summary>
      /// Configures NServiceBus to persists fault information in database using named NHibernate configuration section.
      /// Provided section should be of type <see cref="NHibernate.Cfg.ConfigurationSectionHandler"/>.
      /// </summary>
      /// <param name="config"></param>
      /// <param name="hibernateSectionName">Name of NHibernate configuration section to use.</param>
      /// <param name="autoUpdateSchema">Should fault information tables be re-created at start time?</param>
      /// <returns></returns>
      public static Configure NHibernateFaultManager(this Configure config, string hibernateSectionName, bool autoUpdateSchema)
      {
         if (hibernateSectionName == null)
         {
            throw new ArgumentNullException("hibernateSectionName");
         }
         config.Configurer.RegisterSingleton<FaultManagerSessionFactory>(
            CreateSessionFactory(new MultiConfiguration().ConfigureFromNamedSection(hibernateSectionName), autoUpdateSchema));
         config.Configurer.ConfigureComponent<FaultManager>(DependencyLifecycle.SingleInstance);
         return config;
      }

      /// <summary>
      /// Configures NServiceBus to persists fault information in SQLite database. Persistence schema will be automatically generated.
      /// </summary>
      /// <param name="config"></param>
      /// <returns></returns>
      public static Configure NHibernateFaultManagerWithSQLiteAndAutomaticSchemaGeneration(this Configure config)
      {
        var configuration = new Configuration()
          .DataBaseIntegration(x =>
          {
            x.Dialect<SQLiteDialect>();
            x.ConnectionString = string.Format(@"Data Source={0};Version=3;New=True;", ".\\NServiceBus.Sagas.sqlite");
          });

        //Configuration configuration = new Configuration().SetProperties(
        //    SQLiteConfiguration.Standard
        //       .UsingFile(".\\NServiceBus.Sagas.sqlite")
        //       .ProxyFactoryFactory(typeof (ProxyFactoryFactory).AssemblyQualifiedName).ToProperties());

         config.Configurer.RegisterSingleton<FaultManagerSessionFactory>(
            CreateSessionFactory(configuration, true));
         config.Configurer.ConfigureComponent<FaultManager>(DependencyLifecycle.SingleInstance);
         return config;
      }
      

      internal static FaultManagerSessionFactory CreateSessionFactory(Configuration cfg, bool autoUpdateSchema)
      {
         cfg.AddAssembly(typeof(FailureInfo).Assembly);

         var mapper = new ModelMapper();
         mapper.AddMappings(Assembly.GetExecutingAssembly().GetExportedTypes());
         HbmMapping faultMappings = mapper.CompileMappingForAllExplicitlyAddedEntities();

        cfg.AddMapping(faultMappings);

         //FluentConfiguration fluentConfiguration =
         //   Fluently.Configure(cfg).Mappings(m => m.FluentMappings.Add<FailureInfoMap>());

         //fluentConfiguration.ExposeConfiguration(
         //   c =>
         //   {
         //      //default to LinFu if not specifed by user
         //      if (!c.Properties.Keys.Contains(Environment.ProxyFactoryFactoryClass))
         //      {
         //         c.SetProperty(Environment.ProxyFactoryFactoryClass,
         //                       typeof(ProxyFactoryFactory).AssemblyQualifiedName);
         //      }
         //   });

         if (autoUpdateSchema)
         {
           UpdateDatabaseSchemaUsing(cfg);
         }

         ISessionFactory factory = cfg.BuildSessionFactory();
         return new FaultManagerSessionFactory(factory);
      }

      private static void UpdateDatabaseSchemaUsing(Configuration configuration)
      {
         new SchemaUpdate(configuration)
             .Execute(false, true);
      }
   }
}