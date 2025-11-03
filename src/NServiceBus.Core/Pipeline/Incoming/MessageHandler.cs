#nullable enable

namespace NServiceBus.Pipeline;

using System;
using System.Threading.Tasks;


/// <summary>
/// Non-generic abstraction used by the pipeline.
/// </summary>
public partial class MessageHandler
{
    /// <summary>
    /// The actual instance, can be a saga, a timeout or just a plain handler.
    /// </summary>
    public virtual object? Instance { get; set; }

    /// <summary>
    /// The handler type, can be a saga, a timeout or just a plain handler.
    /// </summary>
    public virtual required Type HandlerType { get; init; }

    internal virtual void Initialize(IServiceProvider provider)
    {
    }

    internal virtual bool IsTimeoutHandler { get; } = false;

    /// <summary>
    /// Invokes the message handler.
    /// </summary>
    /// <param name="message">the message to pass to the handler.</param>
    /// <param name="handlerContext">the context to pass to the handler.</param>
    public virtual Task Invoke(object message, IMessageHandlerContext handlerContext) => Task.CompletedTask;
}