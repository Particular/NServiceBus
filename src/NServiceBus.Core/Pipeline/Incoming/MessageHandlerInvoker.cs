#nullable enable
namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Pipeline;

[DebuggerNonUserCode]
sealed class MessageHandlerInvoker<THandler, TMessage>(
    Func<IServiceProvider, THandler> createHandler,
    Func<THandler, TMessage, IMessageHandlerContext, Task> invocation,
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
    public override required Type HandlerType { get; init; }

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