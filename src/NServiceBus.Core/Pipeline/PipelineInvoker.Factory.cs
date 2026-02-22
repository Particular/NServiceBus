#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using Pipeline;

static partial class PipelineInvoker
{
    static InvokerNode CreateInvokerNode(RegisterStep step, int behaviorIndex, InvokerNode? next)
    {
        if (step.IsTerminator)
        {
            return CreateTerminatingNode(step.InputContextType, behaviorIndex, next);
        }

        return step.IsStageConnector
            ? CreateStageConnectorInvoker(step.InputContextType, step.OutputContextType, behaviorIndex, next)
            : CreateBehaviorInvoker(step.InputContextType, behaviorIndex, next);
    }

    static InvokerNode CreateBehaviorInvoker(Type contextType, int behaviorIndex, InvokerNode? next) =>
        contextType switch
        {
            _ when contextType == typeof(ITransportReceiveContext) => CreateNode<ITransportReceiveContext, ITransportReceiveContext>(behaviorIndex, next),
            _ when contextType == typeof(IIncomingPhysicalMessageContext) => CreateNode<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>(behaviorIndex, next),
            _ when contextType == typeof(IIncomingLogicalMessageContext) => CreateNode<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>(behaviorIndex, next),
            _ when contextType == typeof(IInvokeHandlerContext) => CreateNode<IInvokeHandlerContext, IInvokeHandlerContext>(behaviorIndex, next),
            _ when contextType == typeof(IOutgoingPublishContext) => CreateNode<IOutgoingPublishContext, IOutgoingPublishContext>(behaviorIndex, next),
            _ when contextType == typeof(IOutgoingSendContext) => CreateNode<IOutgoingSendContext, IOutgoingSendContext>(behaviorIndex, next),
            _ when contextType == typeof(IOutgoingReplyContext) => CreateNode<IOutgoingReplyContext, IOutgoingReplyContext>(behaviorIndex, next),
            _ when contextType == typeof(IOutgoingLogicalMessageContext) => CreateNode<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>(behaviorIndex, next),
            _ when contextType == typeof(IOutgoingPhysicalMessageContext) => CreateNode<IOutgoingPhysicalMessageContext, IOutgoingPhysicalMessageContext>(behaviorIndex, next),
            _ when contextType == typeof(IRoutingContext) => CreateNode<IRoutingContext, IRoutingContext>(behaviorIndex, next),
            _ when contextType == typeof(IDispatchContext) => CreateNode<IDispatchContext, IDispatchContext>(behaviorIndex, next),
            _ when contextType == typeof(ISubscribeContext) => CreateNode<ISubscribeContext, ISubscribeContext>(behaviorIndex, next),
            _ when contextType == typeof(IUnsubscribeContext) => CreateNode<IUnsubscribeContext, IUnsubscribeContext>(behaviorIndex, next),
            _ when contextType == typeof(IRecoverabilityContext) => CreateNode<IRecoverabilityContext, IRecoverabilityContext>(behaviorIndex, next),
            _ when contextType == typeof(IBatchDispatchContext) => CreateNode<IBatchDispatchContext, IBatchDispatchContext>(behaviorIndex, next),
            _ when contextType == typeof(IAuditContext) => CreateNode<IAuditContext, IAuditContext>(behaviorIndex, next),
            _ => ThrowUnknownBehaviorContext(contextType)
        };

    static InvokerNode CreateStageConnectorInvoker(Type inputContextType, Type outputContextType, int behaviorIndex, InvokerNode? next)
    {
        if (inputContextType == typeof(ITransportReceiveContext) && outputContextType == typeof(IIncomingPhysicalMessageContext))
        {
            return CreateNode<ITransportReceiveContext, IIncomingPhysicalMessageContext>(behaviorIndex, next);
        }

        if (inputContextType == typeof(IIncomingPhysicalMessageContext) && outputContextType == typeof(IIncomingLogicalMessageContext))
        {
            return CreateNode<IIncomingPhysicalMessageContext, IIncomingLogicalMessageContext>(behaviorIndex, next);
        }

        if (inputContextType == typeof(IIncomingLogicalMessageContext) && outputContextType == typeof(IInvokeHandlerContext))
        {
            return CreateNode<IIncomingLogicalMessageContext, IInvokeHandlerContext>(behaviorIndex, next);
        }

        if (inputContextType == typeof(IOutgoingPublishContext) && outputContextType == typeof(IOutgoingLogicalMessageContext))
        {
            return CreateNode<IOutgoingPublishContext, IOutgoingLogicalMessageContext>(behaviorIndex, next);
        }

        if (inputContextType == typeof(IOutgoingSendContext) && outputContextType == typeof(IOutgoingLogicalMessageContext))
        {
            return CreateNode<IOutgoingSendContext, IOutgoingLogicalMessageContext>(behaviorIndex, next);
        }

        if (inputContextType == typeof(IOutgoingReplyContext) && outputContextType == typeof(IOutgoingLogicalMessageContext))
        {
            return CreateNode<IOutgoingReplyContext, IOutgoingLogicalMessageContext>(behaviorIndex, next);
        }

        if (inputContextType == typeof(IOutgoingLogicalMessageContext) && outputContextType == typeof(IOutgoingPhysicalMessageContext))
        {
            return CreateNode<IOutgoingLogicalMessageContext, IOutgoingPhysicalMessageContext>(behaviorIndex, next);
        }

        if (inputContextType == typeof(IOutgoingPhysicalMessageContext) && outputContextType == typeof(IRoutingContext))
        {
            return CreateNode<IOutgoingPhysicalMessageContext, IRoutingContext>(behaviorIndex, next);
        }

        if (inputContextType == typeof(IAuditContext) && outputContextType == typeof(IRoutingContext))
        {
            return CreateNode<IAuditContext, IRoutingContext>(behaviorIndex, next);
        }

        if (inputContextType == typeof(IRoutingContext) && outputContextType == typeof(IDispatchContext))
        {
            return CreateNode<IRoutingContext, IDispatchContext>(behaviorIndex, next);
        }

        if (inputContextType == typeof(IBatchDispatchContext) && outputContextType == typeof(IDispatchContext))
        {
            return CreateNode<IBatchDispatchContext, IDispatchContext>(behaviorIndex, next);
        }

        if (inputContextType == typeof(IRecoverabilityContext) && outputContextType == typeof(IRoutingContext))
        {
            return CreateNode<IRecoverabilityContext, IRoutingContext>(behaviorIndex, next);
        }

        return ThrowUnknownStageConnector(inputContextType, outputContextType);
    }

    static InvokerNode CreateTerminatingNode(Type inputContextType, int behaviorIndex, InvokerNode? next) =>
        inputContextType switch
        {
            _ when inputContextType == typeof(ITransportReceiveContext) => CreateNode<ITransportReceiveContext, IBehaviorContext>(behaviorIndex, next),
            _ when inputContextType == typeof(IIncomingPhysicalMessageContext) => CreateNode<IIncomingPhysicalMessageContext, IBehaviorContext>(behaviorIndex, next),
            _ when inputContextType == typeof(IIncomingLogicalMessageContext) => CreateNode<IIncomingLogicalMessageContext, IBehaviorContext>(behaviorIndex, next),
            _ when inputContextType == typeof(IInvokeHandlerContext) => CreateNode<IInvokeHandlerContext, IBehaviorContext>(behaviorIndex, next),
            _ when inputContextType == typeof(IOutgoingPublishContext) => CreateNode<IOutgoingPublishContext, IBehaviorContext>(behaviorIndex, next),
            _ when inputContextType == typeof(IOutgoingSendContext) => CreateNode<IOutgoingSendContext, IBehaviorContext>(behaviorIndex, next),
            _ when inputContextType == typeof(IOutgoingReplyContext) => CreateNode<IOutgoingReplyContext, IBehaviorContext>(behaviorIndex, next),
            _ when inputContextType == typeof(IOutgoingLogicalMessageContext) => CreateNode<IOutgoingLogicalMessageContext, IBehaviorContext>(behaviorIndex, next),
            _ when inputContextType == typeof(IOutgoingPhysicalMessageContext) => CreateNode<IOutgoingPhysicalMessageContext, IBehaviorContext>(behaviorIndex, next),
            _ when inputContextType == typeof(IRoutingContext) => CreateNode<IRoutingContext, IBehaviorContext>(behaviorIndex, next),
            _ when inputContextType == typeof(IDispatchContext) => CreateNode<IDispatchContext, IBehaviorContext>(behaviorIndex, next),
            _ when inputContextType == typeof(ISubscribeContext) => CreateNode<ISubscribeContext, IBehaviorContext>(behaviorIndex, next),
            _ when inputContextType == typeof(IUnsubscribeContext) => CreateNode<IUnsubscribeContext, IBehaviorContext>(behaviorIndex, next),
            _ when inputContextType == typeof(IRecoverabilityContext) => CreateNode<IRecoverabilityContext, IBehaviorContext>(behaviorIndex, next),
            _ when inputContextType == typeof(IBatchDispatchContext) => CreateNode<IBatchDispatchContext, IBehaviorContext>(behaviorIndex, next),
            _ when inputContextType == typeof(IAuditContext) => CreateNode<IAuditContext, IBehaviorContext>(behaviorIndex, next),
            _ => ThrowUnknownTerminatingContext(inputContextType)
        };

    [DoesNotReturn]
    static InvokerNode ThrowUnknownBehaviorContext(Type contextType) => throw new InvalidOperationException($"Unknown behavior context type '{contextType.FullName}'.");

    [DoesNotReturn]
    static InvokerNode ThrowUnknownStageConnector(Type inContextType, Type outContextType) => throw new InvalidOperationException($"Unknown stage with '{inContextType.FullName}' and '{outContextType.FullName}'.");

    [DoesNotReturn]
    static InvokerNode ThrowUnknownTerminatingContext(Type inputContextType) => throw new InvalidOperationException($"Unknown terminating stage input context '{inputContextType.FullName}'.");
}