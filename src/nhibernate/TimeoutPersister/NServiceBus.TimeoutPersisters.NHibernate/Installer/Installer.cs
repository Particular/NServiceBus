namespace NServiceBus.TimeoutPersisters.NHibernate.Installer
{
    using System.Security.Principal;
    using Config;
    using NServiceBus.Installation;
    using NServiceBus.Installation.Environments;
    using Persistence.NHibernate;
    using global::NHibernate.Cfg;
    using global::NHibernate.Tool.hbm2ddl;

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
        /// <param name="identity">The <see cref="WindowsIdentity"/> to run the installer under.</param>
        public void Install(WindowsIdentity identity)
        {
            if (RunInstaller)
            {
                ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(ConfigureNHibernate.TimeoutPersisterProperties);

                var configuration = new Configuration().AddProperties(ConfigureNHibernate.TimeoutPersisterProperties);
                ConfigureNHibernate.AddMappings<TimeoutEntityMap>(configuration);
                new SchemaUpdate(configuration).Execute(false, true);
            }
        }
    }
}
