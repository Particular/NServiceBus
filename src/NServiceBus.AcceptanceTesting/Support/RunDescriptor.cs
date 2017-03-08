namespace NServiceBus.AcceptanceTesting.Support
{
    public class RunDescriptor
    {
        public RunDescriptor()
        {
            Settings = new RunSettings();
        }

        public RunSettings Settings { get; }

        public ScenarioContext ScenarioContext { get; set; }
    }
}