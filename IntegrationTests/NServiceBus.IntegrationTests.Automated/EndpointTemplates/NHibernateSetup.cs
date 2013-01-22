namespace NServiceBus.IntegrationTests.Automated.EndpointTemplates
{
    using System.Collections.Generic;
    using Support;

    public class NHibernateSetup : IEndpointSetupTemplate
    {
        public void Setup(Configure config, IDictionary<string, string> settings)
        {
            config.UseNHibernateTimeoutPersister();
        }
    }
}