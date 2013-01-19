namespace NServiceBus.IntegrationTests.Automated.EndpointTemplates
{
    using System;
    using Support;

    public class DefaultServer : IEndpointSetupTemplate
    {
        public Action<Configure> Setup()
        {
            return c => c.DefaultBuilder()
                         .XmlSerializer()
                         .UnicastBus();
        }
    }
}