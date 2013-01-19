namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System.Collections.Generic;

    public class RunDescriptor
    {
        public string Name { get; set; }

        public IDictionary<string, string> Settings { get; set; } 
    }
}