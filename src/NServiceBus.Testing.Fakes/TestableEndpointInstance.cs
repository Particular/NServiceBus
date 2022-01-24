namespace NServiceBus.Testing
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A testable implementation of <see cref="IEndpointInstance" />.
    /// </summary>
    public partial class TestableEndpointInstance : TestableMessageSession, IEndpointInstance
    {
        /// <summary>
        /// Indicates whether <see cref="Stop" /> has been called or not.
        /// </summary>
        public bool EndpointStopped { get; private set; }

        /// <summary>
        /// Stops the endpoint.
        /// </summary>
        public virtual Task Stop(CancellationToken cancellationToken = default)
        {
            EndpointStopped = true;
            return Task.FromResult(0);
        }

#pragma warning disable PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
        public ValueTask DisposeAsync() => new ValueTask(Stop());
#pragma warning restore PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
    }
}