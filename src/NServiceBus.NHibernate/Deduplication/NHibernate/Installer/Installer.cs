namespace NServiceBus.Deduplication.NHibernate.Installer
{
    using Config;
    using global::NHibernate.Tool.hbm2ddl;
    using Installation;
    using Installation.Environments;
    using Persistence.NHibernate;

    /// <summary>
    /// Installer for <see cref="Deduplication"/>
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
            if (!RunInstaller)
                return;

            ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(ConfigureNHibernate.GatewayDeduplicationProperties);

            var configuration = ConfigureNHibernate.CreateConfigurationWith(ConfigureNHibernate.GatewayDeduplicationProperties);
            ConfigureNHibernate.AddMappings<DeduplicationMessageMap>(configuration);
            new SchemaUpdate(configuration).Execute(false, true);
        }
    }
}
