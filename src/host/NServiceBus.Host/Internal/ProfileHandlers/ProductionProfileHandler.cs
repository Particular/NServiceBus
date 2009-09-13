namespace NServiceBus.Host.Internal.ProfileHandlers
{
    /// <summary>
    /// Configures the infrastructure for the Production profile.
    /// </summary>
    public class ProductionProfileHandler : IConfigureTheBusForProfile<Production>
    {
        void IConfigureTheBus.Configure(IConfigureThisEndpoint specifier)
        {
            NServiceBus.Configure.With()
                .SpringBuilder()
                .XmlSerializer()
                .Sagas()
                .NHibernateSagaPersister();

            if (specifier is AsA_Publisher)
                Configure.Instance.DBSubcriptionStorage();
        }
    }
}
