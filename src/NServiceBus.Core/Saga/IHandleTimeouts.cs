namespace NServiceBus.Saga
{
    using JetBrains.Annotations;

    /// <summary>
    /// Tells the infrastructure that the user wants to handle a timeout of T
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IHandleTimeouts<T>
    {
        /// <summary>
        /// Called when the timeout has expired
        /// </summary>
        void Timeout(T state);
    }
}