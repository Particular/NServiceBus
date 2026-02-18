#nullable enable

namespace NServiceBus;

using System;
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
        var frame = ctx.Extensions.Frame;
        var nextIndex = ctx.Extensions.AdvanceFrame(out var reachedEnd);
        if (reachedEnd)
        {
            ctx.Extensions.Frame = frame;
            return Task.CompletedTask;
        }

        Task task;
        try
        {
            task = Dispatch(ctx, nextIndex);
        }
#pragma warning disable PS0019
        catch (Exception)
#pragma warning restore PS0019
        {
            ctx.Extensions.Frame = frame;
            throw;
        }

        if (!task.IsCompletedSuccessfully)
        {
            return AwaitAndRestore(task, ctx, frame);
        }

        ctx.Extensions.Frame = frame;
        return task;
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

    [DebuggerStepThrough]
    [DebuggerHidden]
    [DebuggerNonUserCode]
    static async Task AwaitAndRestore(Task task, IBehaviorContext ctx, PipelineFrame frame)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        finally
        {
            ctx.Extensions.Frame = frame;
        }
    }
}