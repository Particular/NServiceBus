#nullable enable

namespace NServiceBus.Pipeline;

using System;
using System.Threading.Tasks;

/// <summary>
/// Represents a message handler and its invocation.
/// </summary>
public class MessageHandler
{
    /// <summary>
    /// Creates a new instance of the message handler with predefined invocation delegate and handler type.
    /// </summary>
    /// <param name="invocation">The invocation with context delegate.</param>
    /// <param name="handlerType">The handler type.</param>
    public MessageHandler(Func<object, object, IMessageHandlerContext, Task> invocation, Type handlerType)
    {
        HandlerType = handlerType;
        this.invocation = invocation;
    }

    internal MessageHandler(Func<IServiceProvider, object> createHandler, Func<object, object, IMessageHandlerContext, Task> invocation, Type handlerType)
        : this(invocation, handlerType) => this.createHandler = createHandler;

    /// <summary>
    /// The actual instance, can be a saga, a timeout or just a plain handler.
    /// </summary>
    public object? Instance { get; set; }

    /// <summary>
    /// The handler type, can be a saga, a timeout or just a plain handler.
    /// </summary>
    public Type HandlerType { get; }

    internal void CreateHandler(IServiceProvider provider)
    {
        if (createHandler is null)
        {
            throw new Exception("TEMP: Could not create handler from handler delegate");
        }

        Instance = createHandler(provider);
    }

    internal bool IsTimeoutHandler { get; init; }

    /// <summary>
    /// Invokes the message handler.
    /// </summary>
    /// <param name="message">the message to pass to the handler.</param>
    /// <param name="handlerContext">the context to pass to the handler.</param>
    public Task Invoke(object message, IMessageHandlerContext handlerContext)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(handlerContext);

        return Instance is null
            ? throw new Exception("Cannot invoke handler because MessageHandler Instance is not set.")
            : invocation(Instance, message, handlerContext);
    }

    readonly Func<object, object, IMessageHandlerContext, Task> invocation;
    readonly Func<IServiceProvider, object>? createHandler;
}