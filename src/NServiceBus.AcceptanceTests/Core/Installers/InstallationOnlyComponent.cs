namespace NServiceBus.AcceptanceTests.Core.Installers;

using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using AcceptanceTesting.Support;
using Configuration.AdvancedExtensibility;
using Installation;
using Microsoft.Extensions.DependencyInjection;

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
        return new InstallationRunner(endpointConfiguration);
    }

    static void RegisterScenarioContext(EndpointConfiguration endpointConfiguration, ScenarioContext scenarioContext)
    {
        var type = scenarioContext.GetType();
        while (type != typeof(object))
        {
            var currentType = type;
            endpointConfiguration.GetSettings().Set(currentType.FullName, scenarioContext);
            endpointConfiguration.RegisterComponents(serviceCollection => serviceCollection.AddSingleton(currentType, scenarioContext));
            type = type.BaseType;
        }
    }

    public class InstallationRunner : ComponentRunner
    {
        EndpointConfiguration endpointConfiguration;

        public InstallationRunner(EndpointConfiguration endpointConfiguration)
        {
            this.endpointConfiguration = endpointConfiguration;
        }

        public override string Name => "Installation only runner";

        public override Task Start(CancellationToken cancellationToken = default) => Installer.Setup(endpointConfiguration, cancellationToken);
    }
}