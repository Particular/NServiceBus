namespace NServiceBus.AcceptanceTesting.Customization;

using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the scenario context, and it's hierarchy on the provided service collection.
        /// </summary>
        public void AddScenarioContext(ScenarioContext scenarioContext)
        {
            var type = scenarioContext.GetType();
            while (type != typeof(object) && type is not null)
            {
                services.AddSingleton(type, scenarioContext);
                type = type.BaseType;
            }
        }
    }
}