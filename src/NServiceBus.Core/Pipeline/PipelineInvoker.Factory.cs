#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using Pipeline;

static partial class PipelineInvoker
{
    static InvokerNode CreateInvokerNode(RegisterStep step, IBehavior behavior, InvokerNode? next)
    {
        if (step.IsTerminator)
        {
            return CreateTerminatingNode(step.InputContextType, behavior, next);
        }

        return step.IsStageConnector
            ? CreateStageConnectorInvoker(step.InputContextType, step.OutputContextType, behavior, next)
            : CreateBehaviorInvoker(step.InputContextType, behavior, next);
    }

    static InvokerNode CreateBehaviorInvoker(Type contextType, IBehavior behavior, InvokerNode? next) =>
        contextType switch
        {
            _ when contextType == typeof(ITransportReceiveContext) => CreateNode<ITransportReceiveContext, ITransportReceiveContext>(behavior, next),
            _ when contextType == typeof(IIncomingPhysicalMessageContext) => CreateNode<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>(behavior, next),
            _ when contextType == typeof(IIncomingLogicalMessageContext) => CreateNode<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>(behavior, next),
            _ when contextType == typeof(IInvokeHandlerContext) => CreateNode<IInvokeHandlerContext, IInvokeHandlerContext>(behavior, next),
            _ when contextType == typeof(IOutgoingPublishContext) => CreateNode<IOutgoingPublishContext, IOutgoingPublishContext>(behavior, next),
            _ when contextType == typeof(IOutgoingSendContext) => CreateNode<IOutgoingSendContext, IOutgoingSendContext>(behavior, next),
            _ when contextType == typeof(IOutgoingReplyContext) => CreateNode<IOutgoingReplyContext, IOutgoingReplyContext>(behavior, next),
            _ when contextType == typeof(IOutgoingLogicalMessageContext) => CreateNode<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>(behavior, next),
            _ when contextType == typeof(IOutgoingPhysicalMessageContext) => CreateNode<IOutgoingPhysicalMessageContext, IOutgoingPhysicalMessageContext>(behavior, next),
            _ when contextType == typeof(IRoutingContext) => CreateNode<IRoutingContext, IRoutingContext>(behavior, next),
            _ when contextType == typeof(IDispatchContext) => CreateNode<IDispatchContext, IDispatchContext>(behavior, next),
            _ when contextType == typeof(ISubscribeContext) => CreateNode<ISubscribeContext, ISubscribeContext>(behavior, next),
            _ when contextType == typeof(IUnsubscribeContext) => CreateNode<IUnsubscribeContext, IUnsubscribeContext>(behavior, next),
            _ when contextType == typeof(IRecoverabilityContext) => CreateNode<IRecoverabilityContext, IRecoverabilityContext>(behavior, next),
            _ when contextType == typeof(IBatchDispatchContext) => CreateNode<IBatchDispatchContext, IBatchDispatchContext>(behavior, next),
            _ when contextType == typeof(IAuditContext) => CreateNode<IAuditContext, IAuditContext>(behavior, next),
            _ => ThrowUnknownBehaviorContext(contextType)
        };

    static InvokerNode CreateStageConnectorInvoker(Type inputContextType, Type outputContextType, IBehavior behavior, InvokerNode? next)
    {
        if (inputContextType == typeof(ITransportReceiveContext) && outputContextType == typeof(IIncomingPhysicalMessageContext))
        {
            return CreateNode<ITransportReceiveContext, IIncomingPhysicalMessageContext>(behavior, next);
        }

        if (inputContextType == typeof(IIncomingPhysicalMessageContext) && outputContextType == typeof(IIncomingLogicalMessageContext))
        {
            return CreateNode<IIncomingPhysicalMessageContext, IIncomingLogicalMessageContext>(behavior, next);
        }

        if (inputContextType == typeof(IIncomingLogicalMessageContext) && outputContextType == typeof(IInvokeHandlerContext))
        {
            return CreateNode<IIncomingLogicalMessageContext, IInvokeHandlerContext>(behavior, next);
        }

        if (inputContextType == typeof(IOutgoingPublishContext) && outputContextType == typeof(IOutgoingLogicalMessageContext))
        {
            return CreateNode<IOutgoingPublishContext, IOutgoingLogicalMessageContext>(behavior, next);
        }

        if (inputContextType == typeof(IOutgoingSendContext) && outputContextType == typeof(IOutgoingLogicalMessageContext))
        {
            return CreateNode<IOutgoingSendContext, IOutgoingLogicalMessageContext>(behavior, next);
        }

        if (inputContextType == typeof(IOutgoingReplyContext) && outputContextType == typeof(IOutgoingLogicalMessageContext))
        {
            return CreateNode<IOutgoingReplyContext, IOutgoingLogicalMessageContext>(behavior, next);
        }

        if (inputContextType == typeof(IOutgoingLogicalMessageContext) && outputContextType == typeof(IOutgoingPhysicalMessageContext))
        {
            return CreateNode<IOutgoingLogicalMessageContext, IOutgoingPhysicalMessageContext>(behavior, next);
        }

        if (inputContextType == typeof(IOutgoingPhysicalMessageContext) && outputContextType == typeof(IRoutingContext))
        {
            return CreateNode<IOutgoingPhysicalMessageContext, IRoutingContext>(behavior, next);
        }

        if (inputContextType == typeof(IAuditContext) && outputContextType == typeof(IRoutingContext))
        {
            return CreateNode<IAuditContext, IRoutingContext>(behavior, next);
        }

        if (inputContextType == typeof(IRoutingContext) && outputContextType == typeof(IDispatchContext))
        {
            return CreateNode<IRoutingContext, IDispatchContext>(behavior, next);
        }

        if (inputContextType == typeof(IBatchDispatchContext) && outputContextType == typeof(IDispatchContext))
        {
            return CreateNode<IBatchDispatchContext, IDispatchContext>(behavior, next);
        }

        if (inputContextType == typeof(IRecoverabilityContext) && outputContextType == typeof(IRoutingContext))
        {
            return CreateNode<IRecoverabilityContext, IRoutingContext>(behavior, next);
        }

        return ThrowUnknownStageConnector(inputContextType, outputContextType);
    }

    static InvokerNode CreateTerminatingNode(Type inputContextType, IBehavior behavior, InvokerNode? next) =>
        inputContextType switch
        {
            _ when inputContextType == typeof(ITransportReceiveContext) => CreateNode<ITransportReceiveContext, IBehaviorContext>(behavior, next),
            _ when inputContextType == typeof(IIncomingPhysicalMessageContext) => CreateNode<IIncomingPhysicalMessageContext, IBehaviorContext>(behavior, next),
            _ when inputContextType == typeof(IIncomingLogicalMessageContext) => CreateNode<IIncomingLogicalMessageContext, IBehaviorContext>(behavior, next),
            _ when inputContextType == typeof(IInvokeHandlerContext) => CreateNode<IInvokeHandlerContext, IBehaviorContext>(behavior, next),
            _ when inputContextType == typeof(IOutgoingPublishContext) => CreateNode<IOutgoingPublishContext, IBehaviorContext>(behavior, next),
            _ when inputContextType == typeof(IOutgoingSendContext) => CreateNode<IOutgoingSendContext, IBehaviorContext>(behavior, next),
            _ when inputContextType == typeof(IOutgoingReplyContext) => CreateNode<IOutgoingReplyContext, IBehaviorContext>(behavior, next),
            _ when inputContextType == typeof(IOutgoingLogicalMessageContext) => CreateNode<IOutgoingLogicalMessageContext, IBehaviorContext>(behavior, next),
            _ when inputContextType == typeof(IOutgoingPhysicalMessageContext) => CreateNode<IOutgoingPhysicalMessageContext, IBehaviorContext>(behavior, next),
            _ when inputContextType == typeof(IRoutingContext) => CreateNode<IRoutingContext, IBehaviorContext>(behavior, next),
            _ when inputContextType == typeof(IDispatchContext) => CreateNode<IDispatchContext, IBehaviorContext>(behavior, next),
            _ when inputContextType == typeof(ISubscribeContext) => CreateNode<ISubscribeContext, IBehaviorContext>(behavior, next),
            _ when inputContextType == typeof(IUnsubscribeContext) => CreateNode<IUnsubscribeContext, IBehaviorContext>(behavior, next),
            _ when inputContextType == typeof(IRecoverabilityContext) => CreateNode<IRecoverabilityContext, IBehaviorContext>(behavior, next),
            _ when inputContextType == typeof(IBatchDispatchContext) => CreateNode<IBatchDispatchContext, IBehaviorContext>(behavior, next),
            _ when inputContextType == typeof(IAuditContext) => CreateNode<IAuditContext, IBehaviorContext>(behavior, next),
            _ => ThrowUnknownTerminatingContext(inputContextType)
        };

    [DoesNotReturn]
    static InvokerNode ThrowUnknownBehaviorContext(Type contextType) => throw new InvalidOperationException($"Unknown behavior context type '{contextType.FullName}'.");

    [DoesNotReturn]
    static InvokerNode ThrowUnknownStageConnector(Type inContextType, Type outContextType) => throw new InvalidOperationException($"Unknown stage with '{inContextType.FullName}' and '{outContextType.FullName}'.");

    [DoesNotReturn]
    static InvokerNode ThrowUnknownTerminatingContext(Type inputContextType) => throw new InvalidOperationException($"Unknown terminating stage input context '{inputContextType.FullName}'.");
}