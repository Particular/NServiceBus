#pragma warning disable 1591
namespace NServiceBus
{
    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", Replacement = "use IWantCustomInitialization and configure logging before you call Configure.With().", RemoveInVersion = "6.0")]
    public interface IWantCustomLogging
    {
    }
}