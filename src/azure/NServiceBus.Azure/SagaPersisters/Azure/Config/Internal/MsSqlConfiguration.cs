namespace NServiceBus.SagaPersisters.Azure.Config.Internal
{
    using System.Collections.Generic;
    using global::NHibernate.Cfg;
    using global::NHibernate.Drivers.Azure.TableStorage;

    public static class MsSqlConfiguration
  {
    public static IDictionary<string, string> Azure(string connectionString)
    {
      return new Dictionary<string, string>
                 {
                   { Environment.ConnectionProvider, typeof(TableStorageConnectionProvider).AssemblyQualifiedName },
                   { Environment.ConnectionDriver, typeof(TableStorageDriver).AssemblyQualifiedName },
                   { Environment.Dialect, typeof(TableStorageDialect).AssemblyQualifiedName },
                   { Environment.ConnectionString, connectionString },
                 };
    }
  }
}