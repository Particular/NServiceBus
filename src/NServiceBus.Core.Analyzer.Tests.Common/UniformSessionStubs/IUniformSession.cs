namespace NServiceBus.UniformSession
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IUniformSession
    {
        Task Send(object message, SendOptions options, CancellationToken cancellationToken = default);

        Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken = default);

        Task Publish(object message, PublishOptions options, CancellationToken cancellationToken = default);

        Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken = default);
    }
}
