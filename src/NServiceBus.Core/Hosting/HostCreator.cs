namespace NServiceBus
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Settings;

    //TODO move to endpointcreator?
    class HostCreator
    {
        public static EndpointCreator BuildEndpointCreator(EndpointConfiguration endpointConfiguration, IServiceCollection serviceCollection)
        {
            var settings = endpointConfiguration.Settings;

            CheckIfSettingsWhereUsedToCreateAnotherEndpoint(settings);

            var assemblyScanningComponent = AssemblyScanningComponent.Initialize(settings.Get<AssemblyScanningComponent.Configuration>(), settings);

            endpointConfiguration.FinalizeConfiguration(assemblyScanningComponent.AvailableTypes);

            var hostingConfiguration = HostingComponent.PrepareConfiguration(settings.Get<HostingComponent.Settings>(), assemblyScanningComponent, serviceCollection);

            var endpointCreator = EndpointCreator.Create(settings, hostingConfiguration);

            return endpointCreator;
        }

        static void CheckIfSettingsWhereUsedToCreateAnotherEndpoint(SettingsHolder settings)
        {
            if (settings.GetOrDefault<bool>("UsedToCreateEndpoint"))
            {
                throw new ArgumentException("This EndpointConfiguration was already used for starting an endpoint. Each endpoint requires a new EndpointConfiguration.");
            }

            settings.Set("UsedToCreateEndpoint", true);
        }
    }
}
