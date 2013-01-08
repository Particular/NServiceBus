namespace NServiceBus.SagaPersisters.NHibernate.Config.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using AutoPersistence;
    using global::NHibernate;
    using Configuration = global::NHibernate.Cfg.Configuration;
    using Environment = global::NHibernate.Cfg.Environment;

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
        /// <returns></returns>
        public ISessionFactory Build(Configuration nhibernateConfiguration)
        {
            var scannedAssemblies = typesToScan.Select(t => t.Assembly).Distinct();

            foreach (var assembly in scannedAssemblies)
                nhibernateConfiguration.AddAssembly(assembly);

            var modelMapper =
                new SagaModelMapper(typesToScan.Except(nhibernateConfiguration.ClassMappings.Select(x => x.MappedClass)));

            using (var stream = modelMapper.Compile())
            {
                nhibernateConfiguration.AddInputStream(stream);
            }
            
            ApplyDefaultsTo(nhibernateConfiguration);

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

        private static void ApplyDefaultsTo(Configuration configuration)
        {
            configuration.SetProperty(Environment.CurrentSessionContextClass,
                                      typeof(InternalThreadStaticSessionContext).AssemblyQualifiedName);
        }
    }
}