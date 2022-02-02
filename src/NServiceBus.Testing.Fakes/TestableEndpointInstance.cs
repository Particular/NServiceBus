namespace NServiceBus.Testing
{
    using System;
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

#pragma warning disable PS0018
        public virtual ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return new ValueTask(Stop(CancellationToken.None));
        }
#pragma warning restore PS0018
    }
}