using System;

namespace NServiceBus.Host.Internal.ProfileHandlers
{
    /// <summary>
    /// Configures the infrastructure for the Production profile.
    /// </summary>
    public class ProductionProfileHandler : IHandleProfile<Production>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            NServiceBus.Configure.Instance
                .NHibernateSagaPersister();

            if (Config is AsA_Publisher)
                Configure.Instance.DBSubcriptionStorage();
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}
