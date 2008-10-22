
namespace NServiceBus.Saga
{
    public interface HasCompleted
    {
        bool Completed { get; }
    }
}
