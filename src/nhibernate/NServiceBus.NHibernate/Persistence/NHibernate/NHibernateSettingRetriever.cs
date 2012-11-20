namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;

    /// <summary>
    /// Abstraction of a source of configuration data.
    /// </summary>
    public static class NHibernateSettingRetriever
    {
        static NHibernateSettingRetriever()
        {
            AppSettings = () => ConfigurationManager.AppSettings;
            ConnectionStrings = () => ConfigurationManager.ConnectionStrings;
        }

        /// <summary>
        /// Gets the <see cref="AppSettingsSection"/> data for the current application's default configuration.
        /// </summary>
        public static Func<NameValueCollection> AppSettings { get; set; }

        /// <summary>
        /// Gets the <see cref="ConnectionStringsSection"/> data for the current application's default configuration.
        /// </summary>
        public static Func<ConnectionStringSettingsCollection> ConnectionStrings { get; set; }
    }
}