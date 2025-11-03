#nullable enable

namespace NServiceBus.Pipeline;

using System;
using System.Threading.Tasks;


/// <summary>
/// Non-generic abstraction used by the pipeline.
/// </summary>
public abstract class MessageHandler
{
    /// <summary>
    /// The actual instance, can be a saga, a timeout or just a plain handler.
    /// </summary>
    public abstract object? Instance { get; set; }

    /// <summary>
    /// The handler type, can be a saga, a timeout or just a plain handler.
    /// </summary>
    public abstract Type HandlerType { get; }

    internal abstract void Initialize(IServiceProvider provider);

    internal abstract bool IsTimeoutHandler { get; }

    /// <summary>
    /// Invokes the message handler.
    /// </summary>
    /// <param name="message">the message to pass to the handler.</param>
    /// <param name="handlerContext">the context to pass to the handler.</param>
    public abstract Task Invoke(object message, IMessageHandlerContext handlerContext);
}

sealed class MessageHandler<THandler, TMessage>(
    Func<IServiceProvider, THandler> createHandler,
    Func<THandler, TMessage, IMessageHandlerContext, Task> invocation,
    Type handlerType,
    bool isTimeoutHandler)
    : MessageHandler
    where THandler : class
{
    public override object? Instance
    {
        get => instance;
        set =>
            instance = value switch
            {
                null => null,
                THandler handler => handler,
                _ => throw new Exception($"Cannot set instance of type {value.GetType()} to type {typeof(THandler)}.")
            };
    }
    public override Type HandlerType { get; } = handlerType;

    internal override bool IsTimeoutHandler { get; } = isTimeoutHandler;

    internal override void Initialize(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        instance = createHandler(provider);
    }

    public override Task Invoke(object message, IMessageHandlerContext handlerContext)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(handlerContext);

        return instance is null ? throw new Exception("Cannot invoke handler because MessageHandler Instance is not set.") :
            invocation(instance, (TMessage)message, handlerContext);
    }

    THandler? instance;
    readonly Func<IServiceProvider, THandler> createHandler = createHandler ?? throw new ArgumentNullException(nameof(createHandler));
    readonly Func<THandler, TMessage, IMessageHandlerContext, Task> invocation = invocation ?? throw new ArgumentNullException(nameof(invocation));
}