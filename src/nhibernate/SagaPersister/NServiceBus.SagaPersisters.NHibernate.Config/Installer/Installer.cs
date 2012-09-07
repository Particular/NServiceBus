namespace NServiceBus.SagaPersisters.NHibernate.Config.Installer
{
    using System.Linq;
    using System.Security.Principal;
    using AutoPersistence;
    using Installation;
    using Installation.Environments;
    using Persistence.NHibernate;
    using global::NHibernate.Cfg;
    using global::NHibernate.Tool.hbm2ddl;

    /// <summary>
    /// Installer for <see cref="SagaPersister"/>
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
                ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(ConfigureNHibernate.SagaPersisterProperties);

                var configuration = new Configuration().AddProperties(ConfigureNHibernate.SagaPersisterProperties);
                var typesToScan = Configure.TypesToScan.ToList();
                var scannedAssemblies = typesToScan.Select(t => t.Assembly).Distinct();

                foreach (var assembly in scannedAssemblies)
                {
                    configuration.AddAssembly(assembly);
                }

                var modelMapper = new SagaModelMapper(typesToScan.Except(configuration.ClassMappings.Select(x => x.MappedClass)));
                var mapping = modelMapper.Compile();

                configuration.AddMapping(mapping);

                new SchemaUpdate(configuration).Execute(false, true);
            }
        }
    }
}
