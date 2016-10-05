namespace NServiceBus
{
    using JetBrains.Annotations;

    /// <summary>
    /// Marker interface to indicate that a class is a message suitable
    /// for transmission and handling by an NServiceBus.
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IMessage
    {
    }
}