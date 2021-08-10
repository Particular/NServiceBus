namespace NServiceBus.Sagas
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    /// <summary>
    /// Interface indicating that implementers can find sagas
    /// of type <typeparamref name="TSagaData"/> using messages of type <typeparamref name="TMessage"/>.
    /// </summary>
    public interface ISagaFinder<TSagaData, TMessage> : IFinder where TSagaData : IContainSagaData
    {
        /// <summary>
        /// Finds a saga entity of the type <typeparamref name="TSagaData"/> using a message of type <typeparamref name="TMessage"/>.
        /// </summary>
        /// <exception cref="System.Exception">This exception will be thrown if <code>null</code> is returned. Return a Task&lt;T&gt; or mark the method as <code>async</code>.</exception>
        Task<TSagaData> FindBy(TMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context, CancellationToken cancellationToken = default);
    }
}
