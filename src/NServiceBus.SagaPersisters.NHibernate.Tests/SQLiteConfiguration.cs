namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System.Collections.Generic;
    using global::NHibernate.Cfg;

    /// <summary>
    /// SQLite Configuration
    /// </summary>
    public static class SQLiteConfiguration
    {
        /// <summary>
        /// SQLite Configuration In Memory
        /// </summary>
        public static IDictionary<string, string> InMemory()
        {
            var cfg = Base();

            cfg.Add(Environment.ReleaseConnections, "on_close");
            cfg.Add(Environment.ConnectionString, "Data Source=:memory:;Version=3;New=True;");

            return cfg;
        }
        /// <summary>
        /// SQLite Configuration In File
        /// </summary>
        /// <param name="filename">File Name</param>
        public static IDictionary<string, string> UsingFile(string filename)
        {
            var cfg = Base();

            cfg.Add(Environment.ConnectionString, string.Format(@"Data Source={0};Version=3;New=True;", filename));

            return cfg;
        }

        private static IDictionary<string, string> Base()
        {
            return new Dictionary<string, string>
                 {
                   { Environment.ConnectionProvider, typeof(global::NHibernate.Connection.DriverConnectionProvider).FullName },
                   { Environment.ConnectionDriver, typeof(global::NHibernate.Driver.SQLite20Driver).FullName },
                   { Environment.Dialect, typeof(global::NHibernate.Dialect.SQLiteDialect).FullName },
                   { Environment.QuerySubstitutions, "true=1;false=0"},
                 };
        }
    }
}