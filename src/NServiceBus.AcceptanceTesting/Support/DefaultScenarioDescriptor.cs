namespace NServiceBus.AcceptanceTesting.Support
{
    public class DefaultScenarioDescriptor : ScenarioDescriptor
    {
        public DefaultScenarioDescriptor()
        {
            Add(new RunDescriptor { Key = "Default Scenario" });
        }
    }
}