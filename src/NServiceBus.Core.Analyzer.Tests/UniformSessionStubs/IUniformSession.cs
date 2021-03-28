namespace NServiceBus.UniformSession
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    [SuppressMessage("Code", "PCR0019:A task-returning method should have a CancellationToken parameter or a parameter implementing ICancellableContext", Justification = "<Pending>")]
    public interface IUniformSession
    {
        Task Send(object message, SendOptions options);

        Task Send<T>(Action<T> messageConstructor, SendOptions options);

        Task Publish(object message, PublishOptions options);

        Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions);
    }
}
