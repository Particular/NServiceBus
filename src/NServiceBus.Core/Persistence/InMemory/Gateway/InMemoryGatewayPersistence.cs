namespace NServiceBus.Features
{
    /// <summary>
    /// In-memory Gateway.
    /// </summary>
    [ObsoleteEx(
            Message = "Gateway persistence has been moved to the NServiceBus.Gateway dedicated package.",
            RemoveInVersion = "9.0.0",
            TreatAsErrorFromVersion = "8.0.0")]
    public class InMemoryGatewayPersistence : Feature
    {


        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
        }
    }
}