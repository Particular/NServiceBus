namespace NServiceBus.TimeoutPersisters.NHibernate.Installer
{
    using System;
    using Config;
    using global::NHibernate.Tool.hbm2ddl;
    using Installation;
    using Installation.Environments;
    using NServiceBus.Config;
    using Persistence.NHibernate;

    /// <summary>
    /// Installer for <see cref="TimeoutStorage"/>
    /// </summary>
    public class Installer : INeedToInstallSomething<Windows>
    {
        /// <summary>
        /// <value>true</value> to run installer.
        /// </summary>
        public static bool RunInstaller { get; set; }

        /// <summary>
        /// Executes the installer.
        /// </summary>
        /// <param name="identity">The user for whom permissions will be given.</param>
        public void Install(string identity)
        {
            if (RunInstaller)
            {
                var configSection = Configure.GetConfigSection<TimeoutPersisterConfig>();

                if (configSection != null)
                {
                    if (configSection.NHibernateProperties.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "No NHibernate properties found. Please specify NHibernateProperties in your TimeoutPersisterConfig section");
                    }

                    foreach (var property in configSection.NHibernateProperties.ToProperties())
                    {
                        ConfigureNHibernate.TimeoutPersisterProperties[property.Key] = property.Value;
                    }
                }

                ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(ConfigureNHibernate.TimeoutPersisterProperties);

                var configuration = ConfigureNHibernate.CreateConfigurationWith(ConfigureNHibernate.TimeoutPersisterProperties);
                ConfigureNHibernate.AddMappings<TimeoutEntityMap>(configuration);
                new SchemaUpdate(configuration).Execute(false, true);
            }
        }
    }
}
