using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using NHibernate;
using NHibernate.Context;
using NHibernate.Tool.hbm2ddl;
using NServiceBus.SagaPersisters.NHibernate.AutoPersistence;
using Configuration = NHibernate.Cfg.Configuration;

namespace NServiceBus.SagaPersisters.NHibernate.Config.Internal
{
  /// <summary>
  /// Builder class for the NHibernate Session Factory
  /// </summary>
  public class SessionFactoryBuilder
  {
    private readonly IEnumerable<Type> _typesToScan;

    /// <summary>
    /// Constructor that accepts the types to scan for saga data classes
    /// </summary>
    /// <param name="typesToScan"></param>
    public SessionFactoryBuilder(IEnumerable<Type> typesToScan)
    {
      _typesToScan = typesToScan;
    }

    /// <summary>
    /// Builds the session factory with the given properties. Database is updated if updateSchema is set
    /// </summary>
    /// <param name="nhibernateProperties"></param>
    /// <param name="updateSchema"></param>
    /// <returns></returns>
    public ISessionFactory Build(IDictionary<string, string> nhibernateProperties, bool updateSchema)
    {
      var scannedAssemblies = _typesToScan.Select(t => t.Assembly).Distinct();

      var nhibernateConfiguration = new Configuration().SetProperties(nhibernateProperties);

      foreach (var assembly in scannedAssemblies)
        nhibernateConfiguration.AddAssembly(assembly);

      var modelMapper = new SagaModelMapper(_typesToScan.Except(nhibernateConfiguration.ClassMappings.Select(x => x.MappedClass)));

      var mapping = modelMapper.Compile();

      nhibernateConfiguration.AddMapping(mapping);

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