namespace NServiceBus.IntegrationTests.Automated.Support
{
    public class DefaultScenarioDescriptor : ScenarioDescriptor
    {
        public DefaultScenarioDescriptor()
        {
            this.Add(new RunDescriptor { Name = "Default Scenario" });
        }
    }
}