namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System.Collections.Generic;

    public interface IEndpointSetupTemplate
    {
        void Setup(Configure config, IDictionary<string, string> settings);
    }
}