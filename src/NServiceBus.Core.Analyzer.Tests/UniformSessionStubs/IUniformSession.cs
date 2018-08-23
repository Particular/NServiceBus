namespace NServiceBus.UniformSession
{
    using System;
    using System.Threading.Tasks;

    public interface IUniformSession
    {
        Task Send(object message, SendOptions options);

        Task Send<T>(Action<T> messageConstructor, SendOptions options);

        Task Publish(object message, PublishOptions options);

        Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions);
    }
}
