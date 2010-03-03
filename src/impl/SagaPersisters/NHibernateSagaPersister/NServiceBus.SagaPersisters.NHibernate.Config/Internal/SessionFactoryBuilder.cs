using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using FluentNHibernate.Cfg;
using NHibernate;
using NHibernate.ByteCode.LinFu;
using NHibernate.Context;
using NHibernate.Tool.hbm2ddl;
using NServiceBus.SagaPersisters.NHibernate.AutoPersistence;
using Configuration=NHibernate.Cfg.Configuration;

namespace NServiceBus.SagaPersisters.NHibernate.Config.Internal
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
            var model = Create.SagaPersistenceModel(typesToScan);
            var scannedAssemblies = typesToScan.Select(t => t.Assembly).Distinct();

            var nhibernateConfiguration = new Configuration().SetProperties(nhibernateProperties);

            foreach (var assembly in scannedAssemblies)
            {
                nhibernateConfiguration.AddAssembly(assembly);
            }
            
            var fluentConfiguration = Fluently.Configure(nhibernateConfiguration)
                                        .Mappings(m => m.AutoMappings.Add(model));

            ApplyDefaultsTo(fluentConfiguration);
            
            if (updateSchema)
            {
                UpdateDatabaseSchemaUsing(fluentConfiguration);
            }

            try
            {
                return fluentConfiguration.BuildSessionFactory();
            }
            catch (FluentConfigurationException e)
            {
                if (e.InnerException != null)
                    throw new ConfigurationErrorsException(e.InnerException.Message, e);

                throw;
            }
        }

        private static void UpdateDatabaseSchemaUsing(FluentConfiguration fluentConfiguration)
        {
            var configuration = fluentConfiguration.BuildConfiguration();

            new SchemaUpdate(configuration)
                .Execute(false, true);
        }

        private static void ApplyDefaultsTo(FluentConfiguration fluentConfiguration)
        {
            fluentConfiguration.ExposeConfiguration(
                c =>
                    {
                        c.SetProperty("current_session_context_class",typeof(ThreadStaticSessionContext).AssemblyQualifiedName);

                        //default to LinFu if not specifed by user
                        if (!c.Properties.Keys.Contains(PROXY_FACTORY_KEY))
                            c.SetProperty(PROXY_FACTORY_KEY,typeof(ProxyFactoryFactory).AssemblyQualifiedName);
                    }
                );
        }


       
        private const string PROXY_FACTORY_KEY = "proxyfactory.factory_class";
    }
}