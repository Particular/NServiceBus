namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System.Collections.Generic;

    public class RunDescriptor
    {
        public RunDescriptor()
        {
            this.Settings = new Dictionary<string, string>();
        }

        public string Name { get; set; }

        public IDictionary<string, string> Settings { get; set; } 
    }
}