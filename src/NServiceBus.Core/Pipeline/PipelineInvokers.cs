#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Pipeline;

/// <summary>
/// This class is carefully crafted to minimize the amount of code and delegate allocations when invoking behaviors and stages in the pipeline. It uses a combination of switch expressions, generic methods, and caching to achieve this goal.
/// The use of aggressive inlining and debugger attributes helps to further optimize the performance of the pipeline execution while maintaining a good debugging experience.
/// When new pipeline stages are added, the corresponding invoker id and invocation logic should be added to this class to ensure that they are executed with the same level of performance as the existing stages.
/// </summary>
static class PipelineInvokers
{
    [DebuggerStepThrough]
    [DebuggerHidden]
    [DebuggerNonUserCode]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task Invoke(IBehaviorContext ctx, in PipelinePart part) =>
        part.InvokerId switch
        {
            BehaviorTransportReceive => InvokeBehavior<ITransportReceiveContext>(ctx),
            BehaviorIncomingPhysical => InvokeBehavior<IIncomingPhysicalMessageContext>(ctx),
            BehaviorIncomingLogical => InvokeBehavior<IIncomingLogicalMessageContext>(ctx),
            BehaviorInvokeHandler => InvokeBehavior<IInvokeHandlerContext>(ctx),
            BehaviorOutgoingPublish => InvokeBehavior<IOutgoingPublishContext>(ctx),
            BehaviorOutgoingSend => InvokeBehavior<IOutgoingSendContext>(ctx),
            BehaviorOutgoingReply => InvokeBehavior<IOutgoingReplyContext>(ctx),
            BehaviorOutgoingLogical => InvokeBehavior<IOutgoingLogicalMessageContext>(ctx),
            BehaviorOutgoingPhysical => InvokeBehavior<IOutgoingPhysicalMessageContext>(ctx),
            BehaviorRouting => InvokeBehavior<IRoutingContext>(ctx),
            BehaviorDispatch => InvokeBehavior<IDispatchContext>(ctx),
            BehaviorSubscribe => InvokeBehavior<ISubscribeContext>(ctx),
            BehaviorUnsubscribe => InvokeBehavior<IUnsubscribeContext>(ctx),
            BehaviorRecoverability => InvokeBehavior<IRecoverabilityContext>(ctx),
            BehaviorBatchDispatch => InvokeBehavior<IBatchDispatchContext>(ctx),
            BehaviorAudit => InvokeBehavior<IAuditContext>(ctx),

            StageTransportToIncomingPhysical => InvokeStage<ITransportReceiveContext, IIncomingPhysicalMessageContext>(ctx, part.ChildStart, part.ChildEnd),
            StageIncomingPhysicalToIncomingLogical => InvokeStage<IIncomingPhysicalMessageContext, IIncomingLogicalMessageContext>(ctx, part.ChildStart, part.ChildEnd),
            StageIncomingLogicalToInvokeHandler => InvokeStage<IIncomingLogicalMessageContext, IInvokeHandlerContext>(ctx, part.ChildStart, part.ChildEnd),
            StageOutgoingPublishToOutgoingLogical => InvokeStage<IOutgoingPublishContext, IOutgoingLogicalMessageContext>(ctx, part.ChildStart, part.ChildEnd),
            StageOutgoingSendToOutgoingLogical => InvokeStage<IOutgoingSendContext, IOutgoingLogicalMessageContext>(ctx, part.ChildStart, part.ChildEnd),
            StageOutgoingReplyToOutgoingLogical => InvokeStage<IOutgoingReplyContext, IOutgoingLogicalMessageContext>(ctx, part.ChildStart, part.ChildEnd),
            StageOutgoingLogicalToOutgoingPhysical => InvokeStage<IOutgoingLogicalMessageContext, IOutgoingPhysicalMessageContext>(ctx, part.ChildStart, part.ChildEnd),
            StageOutgoingPhysicalToRouting => InvokeStage<IOutgoingPhysicalMessageContext, IRoutingContext>(ctx, part.ChildStart, part.ChildEnd),
            StageAuditToRouting => InvokeStage<IAuditContext, IRoutingContext>(ctx, part.ChildStart, part.ChildEnd),
            StageRoutingToDispatch => InvokeStage<IRoutingContext, IDispatchContext>(ctx, part.ChildStart, part.ChildEnd),
            StageBatchDispatchToDispatch => InvokeStage<IBatchDispatchContext, IDispatchContext>(ctx, part.ChildStart, part.ChildEnd),
            StageRecoverabilityToRouting => InvokeStage<IRecoverabilityContext, IRoutingContext>(ctx, part.ChildStart, part.ChildEnd),
            StageToTerminating => InvokeTerminatingStage(ctx, part),

            _ => ThrownUnknownFallback(ctx, part)
        };

    [DebuggerStepThrough]
    [DebuggerHidden]
    [DebuggerNonUserCode]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Task InvokeBehavior<TContext>(IBehaviorContext ctx)
        where TContext : class, IBehaviorContext
    {
        var behavior = Unsafe.As<IBehavior<TContext, TContext>>(ctx.Extensions.GetBehavior());
        return behavior.Invoke(Unsafe.As<TContext>(ctx), BehaviorNextCache<TContext>.Next);
    }

    [DebuggerStepThrough]
    [DebuggerHidden]
    [DebuggerNonUserCode]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Task InvokeStage<TInContext, TOutContext>(IBehaviorContext ctx, int childStart, int childEnd)
        where TInContext : class, IBehaviorContext
        where TOutContext : class, IBehaviorContext
    {
        ctx.Extensions.SetFrame(childStart - 1, childEnd);
        var behavior = Unsafe.As<IBehavior<TInContext, TOutContext>>(ctx.Extensions.GetBehavior());
        return behavior.Invoke(Unsafe.As<TInContext>(ctx), StageNextCache<TOutContext>.Next);
    }

    static Task InvokeTerminatingStage(IBehaviorContext ctx, in PipelinePart part) =>
        part.ContextTypeName switch
        {
            nameof(ITransportReceiveContext) => InvokeStage<ITransportReceiveContext, IBehaviorContext>(ctx, part.ChildStart, part.ChildEnd),
            nameof(IIncomingPhysicalMessageContext) => InvokeStage<IIncomingPhysicalMessageContext, IBehaviorContext>(ctx, part.ChildStart, part.ChildEnd),
            nameof(IIncomingLogicalMessageContext) => InvokeStage<IIncomingLogicalMessageContext, IBehaviorContext>(ctx, part.ChildStart, part.ChildEnd),
            nameof(IInvokeHandlerContext) => InvokeStage<IInvokeHandlerContext, IBehaviorContext>(ctx, part.ChildStart, part.ChildEnd),
            nameof(IOutgoingPublishContext) => InvokeStage<IOutgoingPublishContext, IBehaviorContext>(ctx, part.ChildStart, part.ChildEnd),
            nameof(IOutgoingSendContext) => InvokeStage<IOutgoingSendContext, IBehaviorContext>(ctx, part.ChildStart, part.ChildEnd),
            nameof(IOutgoingReplyContext) => InvokeStage<IOutgoingReplyContext, IBehaviorContext>(ctx, part.ChildStart, part.ChildEnd),
            nameof(IOutgoingLogicalMessageContext) => InvokeStage<IOutgoingLogicalMessageContext, IBehaviorContext>(ctx, part.ChildStart, part.ChildEnd),
            nameof(IOutgoingPhysicalMessageContext) => InvokeStage<IOutgoingPhysicalMessageContext, IBehaviorContext>(ctx, part.ChildStart, part.ChildEnd),
            nameof(IRoutingContext) => InvokeStage<IRoutingContext, IBehaviorContext>(ctx, part.ChildStart, part.ChildEnd),
            nameof(IDispatchContext) => InvokeStage<IDispatchContext, IBehaviorContext>(ctx, part.ChildStart, part.ChildEnd),
            nameof(ISubscribeContext) => InvokeStage<ISubscribeContext, IBehaviorContext>(ctx, part.ChildStart, part.ChildEnd),
            nameof(IUnsubscribeContext) => InvokeStage<IUnsubscribeContext, IBehaviorContext>(ctx, part.ChildStart, part.ChildEnd),
            nameof(IRecoverabilityContext) => InvokeStage<IRecoverabilityContext, IBehaviorContext>(ctx, part.ChildStart, part.ChildEnd),
            nameof(IBatchDispatchContext) => InvokeStage<IBatchDispatchContext, IBehaviorContext>(ctx, part.ChildStart, part.ChildEnd),
            nameof(IAuditContext) => InvokeStage<IAuditContext, IBehaviorContext>(ctx, part.ChildStart, part.ChildEnd),
            _ => ThrowUnknownTerminatingInput(part.ContextTypeName)
        };

    [DoesNotReturn]
    static Task ThrownUnknownFallback(IBehaviorContext ctx, in PipelinePart part) => throw new InvalidOperationException($"Unknown invoker id '{part.InvokerId}' and no fallback delegate was provided.");

    [DoesNotReturn]
#pragma warning disable PS0018
    static Task ThrowUnknownTerminatingInput(string inputContextName) => throw new InvalidOperationException($"Unknown terminating stage input context '{inputContextName}'.");
#pragma warning restore PS0018

    const byte BehaviorTransportReceive = 1;
    const byte BehaviorIncomingPhysical = 2;
    const byte BehaviorIncomingLogical = 3;
    const byte BehaviorInvokeHandler = 4;
    const byte BehaviorOutgoingPublish = 5;
    const byte BehaviorOutgoingSend = 6;
    const byte BehaviorOutgoingReply = 7;
    const byte BehaviorOutgoingLogical = 8;
    const byte BehaviorOutgoingPhysical = 9;
    const byte BehaviorRouting = 10;
    const byte BehaviorDispatch = 11;
    const byte BehaviorSubscribe = 12;
    const byte BehaviorUnsubscribe = 13;
    const byte BehaviorRecoverability = 14;
    const byte BehaviorBatchDispatch = 15;
    const byte BehaviorAudit = 16;

    const byte StageTransportToIncomingPhysical = 100;
    const byte StageIncomingPhysicalToIncomingLogical = 101;
    const byte StageIncomingLogicalToInvokeHandler = 102;
    const byte StageOutgoingPublishToOutgoingLogical = 103;
    const byte StageOutgoingSendToOutgoingLogical = 104;
    const byte StageOutgoingReplyToOutgoingLogical = 105;
    const byte StageOutgoingLogicalToOutgoingPhysical = 106;
    const byte StageOutgoingPhysicalToRouting = 107;
    const byte StageRoutingToDispatch = 108;
    const byte StageRecoverabilityToRouting = 109;
    const byte StageBatchDispatchToDispatch = 110;
    const byte StageAuditToRouting = 111;
    public const byte StageToTerminating = 120;

    public static byte GetBehaviorId(Type contextType)
    {
        if (contextType == typeof(ITransportReceiveContext))
        {
            return BehaviorTransportReceive;
        }

        if (contextType == typeof(IIncomingPhysicalMessageContext))
        {
            return BehaviorIncomingPhysical;
        }

        if (contextType == typeof(IIncomingLogicalMessageContext))
        {
            return BehaviorIncomingLogical;
        }

        if (contextType == typeof(IInvokeHandlerContext))
        {
            return BehaviorInvokeHandler;
        }

        if (contextType == typeof(IOutgoingPublishContext))
        {
            return BehaviorOutgoingPublish;
        }

        if (contextType == typeof(IOutgoingSendContext))
        {
            return BehaviorOutgoingSend;
        }

        if (contextType == typeof(IOutgoingReplyContext))
        {
            return BehaviorOutgoingReply;
        }

        if (contextType == typeof(IOutgoingLogicalMessageContext))
        {
            return BehaviorOutgoingLogical;
        }

        if (contextType == typeof(IOutgoingPhysicalMessageContext))
        {
            return BehaviorOutgoingPhysical;
        }

        if (contextType == typeof(IRoutingContext))
        {
            return BehaviorRouting;
        }

        if (contextType == typeof(IDispatchContext))
        {
            return BehaviorDispatch;
        }

        if (contextType == typeof(ISubscribeContext))
        {
            return BehaviorSubscribe;
        }

        if (contextType == typeof(IUnsubscribeContext))
        {
            return BehaviorUnsubscribe;
        }

        if (contextType == typeof(IRecoverabilityContext))
        {
            return BehaviorRecoverability;
        }

        if (contextType == typeof(IBatchDispatchContext))
        {
            return BehaviorBatchDispatch;
        }

        if (contextType == typeof(IAuditContext))
        {
            return BehaviorAudit;
        }

        return ThrowUnknownContext(contextType);
    }

    [DoesNotReturn]
    static byte ThrowUnknownContext(Type contextType) => throw new InvalidOperationException($"Unknown behavior context type '{contextType.FullName}'.");

    public static byte GetStageId(Type inContextType, Type outContextType)
    {
        if (inContextType == typeof(ITransportReceiveContext) && outContextType == typeof(IIncomingPhysicalMessageContext))
        {
            return StageTransportToIncomingPhysical;
        }

        if (inContextType == typeof(IIncomingPhysicalMessageContext) && outContextType == typeof(IIncomingLogicalMessageContext))
        {
            return StageIncomingPhysicalToIncomingLogical;
        }

        if (inContextType == typeof(IIncomingLogicalMessageContext) && outContextType == typeof(IInvokeHandlerContext))
        {
            return StageIncomingLogicalToInvokeHandler;
        }

        if (inContextType == typeof(IOutgoingPublishContext) && outContextType == typeof(IOutgoingLogicalMessageContext))
        {
            return StageOutgoingPublishToOutgoingLogical;
        }

        if (inContextType == typeof(IOutgoingSendContext) && outContextType == typeof(IOutgoingLogicalMessageContext))
        {
            return StageOutgoingSendToOutgoingLogical;
        }

        if (inContextType == typeof(IOutgoingReplyContext) && outContextType == typeof(IOutgoingLogicalMessageContext))
        {
            return StageOutgoingReplyToOutgoingLogical;
        }

        if (inContextType == typeof(IOutgoingLogicalMessageContext) && outContextType == typeof(IOutgoingPhysicalMessageContext))
        {
            return StageOutgoingLogicalToOutgoingPhysical;
        }

        if (inContextType == typeof(IOutgoingPhysicalMessageContext) && outContextType == typeof(IRoutingContext))
        {
            return StageOutgoingPhysicalToRouting;
        }

        if (inContextType == typeof(IAuditContext) && outContextType == typeof(IRoutingContext))
        {
            return StageAuditToRouting;
        }

        if (inContextType == typeof(IRoutingContext) && outContextType == typeof(IDispatchContext))
        {
            return StageRoutingToDispatch;
        }

        if (inContextType == typeof(IBatchDispatchContext) && outContextType == typeof(IDispatchContext))
        {
            return StageBatchDispatchToDispatch;
        }

        if (inContextType == typeof(IRecoverabilityContext) && outContextType == typeof(IRoutingContext))
        {
            return StageRecoverabilityToRouting;
        }

        return ThrowUnknownStage(inContextType, outContextType);
    }

    [DoesNotReturn]
    static byte ThrowUnknownStage(Type inContextType, Type outContextType) => throw new InvalidOperationException($"Unknown stage with '{inContextType.FullName}' and '{outContextType.FullName}'.");

    // Caches to avoid unnecessary delegate allocations on each behavior invocation
    static class BehaviorNextCache<TContext> where TContext : class, IBehaviorContext
    {
        public static readonly Func<TContext, Task> Next = PipelineRunner.Next;
    }

    // Caches to avoid unnecessary delegate allocations on each Stage invocation
    static class StageNextCache<TOutContext> where TOutContext : class, IBehaviorContext
    {
        public static readonly Func<TOutContext, Task> Next = PipelineRunner.Next;
    }
}