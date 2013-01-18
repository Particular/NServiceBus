namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System;

    public interface IEndpointSetupTemplate
    {
        Action<Configure> GetSetupAction();
    }
}