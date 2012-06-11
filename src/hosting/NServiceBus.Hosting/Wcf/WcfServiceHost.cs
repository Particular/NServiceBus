using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using NServiceBus.Logging;

namespace NServiceBus.Hosting.Wcf
{
    /// <summary>
    /// A specialized service host that adds a default endpoint if non is specified in config
    /// </summary>
    public class WcfServiceHost : ServiceHost
    {
        /// <summary>
        /// Constructs the host with the given service type
        /// </summary>
        /// <param name="t"></param>
        public WcfServiceHost(Type t)
            : base(t)
        {

        }


        /// <summary>
        /// Adds the given endpoint unless its already configured in app.config
        /// </summary>
        /// <param name="contractType"></param>
        /// <param name="binding"></param>
        /// <param name="address"></param>
        public void AddDefaultEndpoint(Type contractType,Binding binding,string address)
        {
            var serviceModel = ServiceModelSectionGroup.GetSectionGroup(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None));
            
            if(serviceModel == null)
             throw new InvalidOperationException("No service model section found in config");
            
            bool endpointAlreadyConfigured = false;

            foreach (ServiceElement se in serviceModel.Services.Services)
            {
                if (se.Name == Description.ConfigurationName)
                {
                    foreach (ServiceEndpointElement endpoint in se.Endpoints)
                    {
                        if (endpoint.Contract == contractType.FullName && endpoint.Address.OriginalString == address)
                            endpointAlreadyConfigured = true;
                    }

                }
            }
            if (!endpointAlreadyConfigured)
            {
                logger.Debug("Endpoint for contract: " + contractType.Name + " is not found in configuration, going to add it programatically");
                AddServiceEndpoint(contractType, binding, address);
            }
                
        }

        private readonly ILog logger = LogManager.GetLogger(typeof(WcfServiceHost));
    }
}