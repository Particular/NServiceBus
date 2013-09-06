namespace NServiceBus.SagaPersisters.NHibernate.Config.Installer
{
    using System;
    using System.Linq;
    using AutoPersistence;
    using global::NHibernate.Tool.hbm2ddl;
    using Installation;
    using Installation.Environments;
    using NServiceBus.Config;
    using Persistence.NHibernate;

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
        /// <param name="identity">The user for whom permissions will be given.</param>
        public void Install(string identity)
        {
            if (RunInstaller)
            {
                var configSection = Configure.GetConfigSection<NHibernateSagaPersisterConfig>();

                if (configSection != null)
                {
                    if (configSection.NHibernateProperties.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "No NHibernate properties found. Please specify NHibernateProperties in your NHibernateSagaPersisterConfig section");
                    }

                    foreach (var property in configSection.NHibernateProperties.ToProperties())
                    {
                        ConfigureNHibernate.SagaPersisterProperties[property.Key] = property.Value;
                    }
                }

                ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(ConfigureNHibernate.SagaPersisterProperties);

                var configuration = ConfigureNHibernate.CreateConfigurationWith(ConfigureNHibernate.SagaPersisterProperties);
                var typesToScan = Configure.TypesToScan.ToList();
                var scannedAssemblies = typesToScan.Select(t => t.Assembly).Distinct();

                foreach (var assembly in scannedAssemblies)
                {
                    configuration.AddAssembly(assembly);
                }

                var modelMapper = new SagaModelMapper(typesToScan.Except(configuration.ClassMappings.Select(x => x.MappedClass)));
                
                configuration.AddMapping(modelMapper.Compile());


                new SchemaUpdate(configuration).Execute(false, true);
            }
        }
    }
}
