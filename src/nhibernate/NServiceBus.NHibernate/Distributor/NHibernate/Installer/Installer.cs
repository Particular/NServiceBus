namespace NServiceBus.Distributor.NHibernate.Installer
{
    using Config;
    using Installation;
    using Installation.Environments;
    using Persistence.NHibernate;
    using global::NHibernate.Tool.hbm2ddl;

    /// <summary>
    /// Installer for <see cref="NHibernateWorkerAvailabilityManager"/>
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
                ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(ConfigureNHibernate.DistributorPersisterProperties);

                var configuration = ConfigureNHibernate.CreateConfigurationWith(ConfigureNHibernate.DistributorPersisterProperties);
                ConfigureNHibernate.AddMappings<DistributorMessageMap>(configuration);
                new SchemaUpdate(configuration).Execute(false, true);
            }
        }
    }
}
