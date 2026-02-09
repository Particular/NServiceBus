#nullable enable

namespace NServiceBus;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Pipeline;

static class StageRunners
{
    [DebuggerStepThrough]
    [DebuggerHidden]
    [DebuggerNonUserCode]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task Start(IBehaviorContext ctx)
    {
        var extensions = ctx.Extensions;
        ref var frame = ref extensions.Frame;
        frame.Index = 0;
        frame.RangeEnd = extensions.Parts.Length;

        return extensions.Parts.Length == 0 ? Task.CompletedTask : Dispatch(ctx, 0);
    }

    [DebuggerStepThrough]
    [DebuggerHidden]
    [DebuggerNonUserCode]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task Next(IBehaviorContext ctx)
    {
        var extensions = ctx.Extensions;
        ref var frame = ref extensions.Frame;
        var nextIndex = ++frame.Index;

        if ((uint)nextIndex >= (uint)frame.RangeEnd)
        {
            return Task.CompletedTask;
        }

        return Dispatch(ctx, nextIndex);
    }

    [DebuggerStepThrough]
    [DebuggerHidden]
    [DebuggerNonUserCode]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Task Dispatch(IBehaviorContext ctx, int index)
    {
        ref var part = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(ctx.Extensions.Parts), index);
        return part.Invoke(ctx, part.ChildStart, part.ChildEnd);
    }
}
