using System.Collections.Generic;
using NHibernate.Cfg;
using NHibernate.Drivers.Azure.TableStorage;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
  public static class MsSqlConfiguration
  {
    public static IDictionary<string, string> Azure(string connectionString)
    {
      return new Dictionary<string, string>
                 {
                   { Environment.ConnectionProvider, typeof(TableStorageConnectionProvider).FullName },
                   { Environment.ConnectionDriver, typeof(TableStorageDriver).FullName },
                   { Environment.Dialect, typeof(TableStorageDialect).FullName },
                   {Environment.ConnectionString, connectionString},
                 };
    }
  }
}