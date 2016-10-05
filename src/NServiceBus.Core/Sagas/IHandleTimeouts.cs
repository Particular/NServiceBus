namespace NServiceBus
{
    using System.Threading.Tasks;
    using JetBrains.Annotations;

    /// <summary>
    /// Tells the infrastructure that the user wants to handle a timeout of <typeparamref name="T" />.
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IHandleTimeouts<T>
    {
        /// <summary>
        /// Called when the timeout has expired.
        /// </summary>
        /// <exception cref="System.Exception">This exception will be thrown if <code>null</code> is returned. Return a Task or mark the method as <code>async</code>.</exception>
        Task Timeout(T state, IMessageHandlerContext context);
    }
}