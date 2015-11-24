#pragma warning disable 1591
namespace NServiceBus
{
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0", 
        ReplacementTypeOrMember = "config.UseTransport<MsmqTransport>().SubscriptionAuthorizer(Authorizer);")]
    public interface IAuthorizeSubscriptions
    {
    }
}
