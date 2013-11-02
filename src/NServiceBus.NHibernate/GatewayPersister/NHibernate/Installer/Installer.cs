namespace NServiceBus.GatewayPersister.NHibernate.Installer
{
    using Config;
    using global::NHibernate.Tool.hbm2ddl;
    using Installation;
    using Installation.Environments;
    using Persistence.NHibernate;

    /// <summary>
    /// Installer for <see cref="GatewayPersister"/>
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
                ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(ConfigureNHibernate.GatewayPersisterProperties);

                var configuration = ConfigureNHibernate.CreateConfigurationWith(ConfigureNHibernate.GatewayPersisterProperties);
                ConfigureNHibernate.AddMappings<GatewayMessageMap>(configuration);
                new SchemaUpdate(configuration).Execute(false, true);
            }
        }
    }
}
