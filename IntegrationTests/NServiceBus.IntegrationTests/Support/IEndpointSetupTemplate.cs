namespace NServiceBus.IntegrationTests.Support
{
    using System.Collections.Generic;

    public interface IEndpointSetupTemplate
    {
        void Setup(Configure config, IDictionary<string, string> settings);
    }
}