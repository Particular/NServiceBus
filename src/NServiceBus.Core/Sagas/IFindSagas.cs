namespace NServiceBus.Sagas
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    /// <summary>
    /// Interface indicating that implementers can find sagas of the given type.
    /// </summary>
    public abstract class IFindSagas<T> where T : IContainSagaData
    {
        /// <summary>
        /// Narrower interface indicating that implementers can find sagas
        /// of type T using messages of type M.
        /// </summary>
#pragma warning disable IDE1006 // Naming Styles
        public interface Using<M> : IFinder
#pragma warning restore IDE1006 // Naming Styles
        {
            /// <summary>
            /// Finds a saga entity of the type T using a message of type M.
            /// </summary>
            /// <exception cref="System.Exception">This exception will be thrown if <code>null</code> is returned. Return a Task&lt;T&gt; or mark the method as <code>async</code>.</exception>
            Task<T> FindBy(M message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context, CancellationToken cancellationToken = default);
        }
    }
}