namespace NServiceBus.AcceptanceTests.Core.Installers;

using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using AcceptanceTesting.Support;
using Configuration.AdvancedExtensibility;
using Installation;

/// <summary>
/// Custom test component that uses the <see cref="Installer.Setup"/> API instead of fully starting the endpoint.
/// </summary>
public class InstallationOnlyComponent<TConfigurationFactory> : IComponentBehavior
    where TConfigurationFactory : IEndpointConfigurationFactory, new()
{
    public async Task<ComponentRunner> CreateRunner(RunDescriptor run)
    {
        var configurationFactory = new TConfigurationFactory();
        var customizationConfiguration = configurationFactory.Get();
        customizationConfiguration.EndpointName = Conventions.EndpointNamingConvention(configurationFactory.GetType());
        var endpointConfiguration = await customizationConfiguration.GetConfiguration(run);
        RegisterScenarioContext(endpointConfiguration, run.ScenarioContext);
        var installer = Installer.CreateInstallerWithExternallyManagedContainer(endpointConfiguration, run.Services);
        return new InstallationRunner(installer, run);
    }

    static void RegisterScenarioContext(EndpointConfiguration endpointConfiguration, ScenarioContext scenarioContext)
    {
        var type = scenarioContext.GetType();
        var settings = endpointConfiguration.GetSettings();

        while (type != typeof(object))
        {
            var currentType = type;
            settings.Set(currentType.FullName, scenarioContext);
            type = type.BaseType;
        }
    }

    public class InstallationRunner(InstallerWithExternallyManagedContainer installer, RunDescriptor run) : ComponentRunner
    {
        public override string Name => "Installation only runner";

        public override Task Start(CancellationToken cancellationToken = default) => installer.Setup(run.ServiceProvider!, cancellationToken);
    }
}