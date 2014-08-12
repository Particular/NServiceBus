#pragma warning disable 1591
namespace NServiceBus.Saga
{

    [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "NServiceBus.Saga.Saga")]
    public interface ISaga
    {
    }

    [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "NServiceBus.Saga.Saga<T>")]
    public interface ISaga<T>
    {
    }
    [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "ISagaPersister")]
    public interface IPersistSagas
    {
    }
    [ObsoleteEx(Message = "Since ISaga has been merged into the abstract class Saga this interface is no longer required.",RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "NServiceBus.Saga.Saga")]
    public interface IConfigurable
    {
    }
    [ObsoleteEx(Message = "Since ISaga has been merged into the abstract class Saga this interface is no longer required.", RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "NServiceBus.Saga.Saga.Completed")]
    public interface HasCompleted
    {
    }
}