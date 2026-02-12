#nullable enable

namespace NServiceBus;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Pipeline;

static class PipelineRunner
{
    [DebuggerStepThrough]
    [DebuggerHidden]
    [DebuggerNonUserCode]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task Start(IBehaviorContext ctx)
    {
        ctx.Extensions.InitFrame(out var isEmpty);
        return isEmpty ? Task.CompletedTask : Dispatch(ctx, 0);
    }

    [DebuggerStepThrough]
    [DebuggerHidden]
    [DebuggerNonUserCode]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task Next(IBehaviorContext ctx)
    {
        var nextIndex = ctx.Extensions.AdvanceFrame(out var reachedEnd);
        return reachedEnd ? Task.CompletedTask : Dispatch(ctx, nextIndex);
    }

    [DebuggerStepThrough]
    [DebuggerHidden]
    [DebuggerNonUserCode]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Task Dispatch(IBehaviorContext ctx, int index)
    {
        ref var part = ref ctx.Extensions.GetPart(index);
        return PipelineInvokers.Invoke(ctx, part);
    }
}