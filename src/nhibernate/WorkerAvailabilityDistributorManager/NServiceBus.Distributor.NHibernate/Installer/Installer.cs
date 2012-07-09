namespace NServiceBus.Distributor.NHibernate.Installer
{
    using System.Security.Principal;
    using Config;
    using NServiceBus.Installation;
    using NServiceBus.Installation.Environments;
    using NServiceBus.Persistence.NHibernate;
    using global::NHibernate.Cfg;
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
        /// <param name="identity">The <see cref="WindowsIdentity"/> to run the installer under.</param>
        public void Install(WindowsIdentity identity)
        {
            if (RunInstaller)
            {
                ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(ConfigureNHibernate.DistributorPersisterProperties);

                var configuration = new Configuration().AddProperties(ConfigureNHibernate.DistributorPersisterProperties);
                ConfigureNHibernate.AddMappings<DistributorMessageMap>(configuration);
                new SchemaUpdate(configuration).Execute(false, true);
            }
        }
    }
}
