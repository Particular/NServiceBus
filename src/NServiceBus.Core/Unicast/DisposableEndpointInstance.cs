#if NETCOREAPP
namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Contains extension methods to create a <see cref="IDisposableEndpointInstance"/>.
    /// </summary>
    public static class DisposableEndpointExtensions
    {
        /// <summary>
        /// Use this method to turn a starting endpoint instance into a <see cref="IDisposableEndpointInstance"/> that will stop the endpoint automatically on dispose.
        /// If the endpoint is stopped during disposal, it will use a completed <see cref="CancellationToken"/>, indicating that it expects immediate shutdown.
        /// </summary>
#pragma warning disable PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
        public static async Task<IDisposableEndpointInstance> AsDisposable(this Task<IEndpointInstance> startTask)
#pragma warning restore PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
        {
            var runningInstance = await startTask.ConfigureAwait(false);
            return new DisposableEndpointInstance(runningInstance);
        }
    }

    /// <summary>
    /// an <see cref="IEndpointInstance"/> that implements <see cref="IAsyncDisposable"/>. The endpoint performs a graceful shutdown when being disposed.
    /// </summary>
    public interface IDisposableEndpointInstance : IEndpointInstance, IAsyncDisposable
    {
    }

    class DisposableEndpointInstance : IDisposableEndpointInstance
    {
        IEndpointInstance endpointInstance;

        public DisposableEndpointInstance(IEndpointInstance endpointInstance) => this.endpointInstance = endpointInstance;

        ValueTask IAsyncDisposable.DisposeAsync() => new ValueTask(endpointInstance.Stop(CancellationToken.None));

        public Task Send(object message, SendOptions sendOptions, CancellationToken cancellationToken = default) => endpointInstance.Send(message, sendOptions, cancellationToken);

        public Task Send<T>(Action<T> messageConstructor, SendOptions sendOptions, CancellationToken cancellationToken = default) => endpointInstance.Send(messageConstructor, sendOptions, cancellationToken);

        public Task Publish(object message, PublishOptions publishOptions, CancellationToken cancellationToken = default) => endpointInstance.Publish(message, publishOptions, cancellationToken);

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken = default) => endpointInstance.Publish(messageConstructor, publishOptions, cancellationToken);

        public Task Subscribe(Type eventType, SubscribeOptions subscribeOptions, CancellationToken cancellationToken = default) => endpointInstance.Subscribe(eventType, subscribeOptions, cancellationToken);

        public Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions, CancellationToken cancellationToken = default) => endpointInstance.Unsubscribe(eventType, unsubscribeOptions, cancellationToken);

        public Task Stop(CancellationToken cancellationToken = default) => endpointInstance.Stop(cancellationToken);
    }
}
#endif