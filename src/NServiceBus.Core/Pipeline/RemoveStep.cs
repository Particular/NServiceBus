namespace NServiceBus;

sealed class RemoveStep(string removeId)
{
    public string RemoveId { get; } = removeId;
}