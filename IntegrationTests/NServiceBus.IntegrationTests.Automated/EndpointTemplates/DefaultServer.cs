namespace NServiceBus.IntegrationTests.Automated.EndpointTemplates
{
    using System;
    using Support;

    public class DefaultServer : IEndpointSetupTemplate
    {
        public Action<Configure> GetSetupAction()
        {
            return c => c.DefaultBuilder()
                         .XmlSerializer()
                         .MsmqTransport()
                         .UnicastBus();
        }
    }
}