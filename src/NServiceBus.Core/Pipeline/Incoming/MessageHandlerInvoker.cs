#nullable enable
namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;

// Be cautious when renaming this class because it is used in Core acceptance tests to verify it is hidden from the stack traces
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

    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    [StackTraceHidden]
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

// Non-generic invoker to avoid MakeGenericMethod().Invoke() which can cause module initializer
// to fire multiple times in certain debugging scenarios (e.g., Rider debugger)
sealed class ReflectionMessageHandlerInvoker : MessageHandler
{
    readonly MethodInfo handleMethod;

    [SetsRequiredMembers]
#pragma warning disable CS8618 // HandlerType is set via init in this constructor
    public ReflectionMessageHandlerInvoker(Type handlerType, Type messageType, bool isTimeoutHandler)
#pragma warning restore CS8618
    {
        HandlerType = handlerType;
        IsTimeoutHandler = isTimeoutHandler;

        var interfaceType = isTimeoutHandler
            ? typeof(IHandleTimeouts<>).MakeGenericType(messageType)
            : typeof(IHandleMessages<>).MakeGenericType(messageType);

        var methodName = isTimeoutHandler ? nameof(IHandleTimeouts<>.Timeout) : nameof(IHandleMessages<>.Handle);

        // Get the method from the interface map to handle explicit interface implementations
        var map = handlerType.GetInterfaceMap(interfaceType);
        var interfaceMethod = interfaceType.GetMethod(methodName)!;
        var methodIndex = Array.IndexOf(map.InterfaceMethods, interfaceMethod);
        handleMethod = map.TargetMethods[methodIndex];
    }

    public override object? Instance { get; set; }

    public override required Type HandlerType { get; init; }

    internal override bool IsTimeoutHandler { get; }

    internal override void Initialize(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        Instance = ActivatorUtilities.CreateInstance(provider, HandlerType);
    }

    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    [StackTraceHidden]
    public override Task Invoke(object message, IMessageHandlerContext handlerContext)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(handlerContext);

        if (Instance is null)
        {
            throw new Exception("Cannot invoke handler because MessageHandler Instance is not set.");
        }

        return (Task)handleMethod.Invoke(Instance, [message, handlerContext])!;
    }
}