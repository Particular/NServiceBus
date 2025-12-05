namespace NServiceBus.AcceptanceTesting;

using System;
using System.Threading.Tasks;
using Configuration.AdvancedExtensibility;
using Features;
using Support;

public abstract class EndpointConfigurationBuilder : IEndpointConfigurationFactory
{
    protected EndpointConfigurationBuilder() => configuration = new EndpointCustomizationConfiguration { BuilderType = GetType(), };

    public EndpointConfigurationBuilder CustomMachineName(string customMachineName)
    {
        configuration.CustomMachineName = customMachineName;

        return this;
    }

    public EndpointConfigurationBuilder EnableStartupDiagnostics()
    {
        configuration.DisableStartupDiagnostics = false;

        return this;
    }

    public EndpointConfigurationBuilder CustomEndpointName(string customEndpointName)
    {
        configuration.CustomEndpointName = customEndpointName;

        return this;
    }

    EndpointCustomizationConfiguration CreateScenario()
    {
        configuration.BuilderType = GetType();

        return configuration;
    }

    public EndpointConfigurationBuilder EndpointSetup<T>(Action<EndpointConfiguration>? configurationBuilderCustomization = null,
        Action<PublisherMetadata>? publisherMetadata = null) where T : IEndpointSetupTemplate, new()
    {
        configurationBuilderCustomization ??= b => { };

        return EndpointSetup<T>((bc, _) => configurationBuilderCustomization(bc), publisherMetadata);
    }

    public EndpointConfigurationBuilder EndpointSetup<T>(Action<EndpointConfiguration, RunDescriptor> configurationBuilderCustomization, Action<PublisherMetadata>? publisherMetadata = null) where T : IEndpointSetupTemplate, new()
    {
        var template = new T();
        return EndpointSetup(template, configurationBuilderCustomization, publisherMetadata);
    }

    public EndpointConfigurationBuilder EndpointSetup(IEndpointSetupTemplate endpointTemplate, Action<EndpointConfiguration, RunDescriptor> configurationBuilderCustomization, Action<PublisherMetadata>? publisherMetadata = null)
    {
        publisherMetadata?.Invoke(configuration.PublisherMetadata);

        configuration.GetConfiguration = async runDescriptor =>
        {
            var endpointConfiguration = await endpointTemplate.GetConfiguration(runDescriptor, configuration, bc =>
            {
                configurationBuilderCustomization(bc, runDescriptor);
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            if (configuration.DisableStartupDiagnostics)
            {
                endpointConfiguration.GetSettings().Set("NServiceBus.HostStartupDiagnostics", FeatureState.Disabled);
            }

            return endpointConfiguration;
        };

        return this;
    }

    public EndpointConfigurationBuilder EndpointSetup<T, TContext>(Action<EndpointConfiguration, TContext> configurationBuilderCustomization, Action<PublisherMetadata>? publisherMetadata = null)
        where T : IEndpointSetupTemplate, new()
        where TContext : ScenarioContext
    {
        publisherMetadata?.Invoke(configuration.PublisherMetadata);

        configuration.GetConfiguration = async runDescriptor =>
        {
            var endpointSetupTemplate = new T();
            var endpointConfiguration = await endpointSetupTemplate.GetConfiguration(runDescriptor, configuration, bc =>
            {
                configurationBuilderCustomization(bc, (TContext)runDescriptor.ScenarioContext);
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            if (configuration.DisableStartupDiagnostics)
            {
                endpointConfiguration.GetSettings().Set("NServiceBus.HostStartupDiagnostics", FeatureState.Disabled);
            }

            return endpointConfiguration;
        };

        return this;
    }

    EndpointCustomizationConfiguration IEndpointConfigurationFactory.Get() => CreateScenario();

    public ScenarioContext? ScenarioContext { get; set; }

    public EndpointConfigurationBuilder IncludeType<T>()
    {
        configuration.TypesToInclude.Add(typeof(T));

        return this;
    }

    public EndpointConfigurationBuilder DoNotAutoRegisterHandlers()
    {
        configuration.AutoRegisterHandlers = false;

        return this;
    }

    public EndpointConfigurationBuilder DoNotAutoRegisterSagas()
    {
        configuration.AutoRegisterSagas = false;

        return this;
    }

    readonly EndpointCustomizationConfiguration configuration;
}