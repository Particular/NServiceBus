#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Pipeline;

abstract class InvokerNode
{
    [StackTraceHidden]
    [DebuggerStepThrough]
    [DebuggerHidden]
    [DebuggerNonUserCode]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract Task Invoke(IBehaviorContext context);
}

sealed class InvokerNode<TInContext, TOutContext>(IBehavior<TInContext, TOutContext> behavior, Func<TOutContext, Task> next) : InvokerNode
    where TInContext : IBehaviorContext
    where TOutContext : IBehaviorContext
{
    [StackTraceHidden]
    [DebuggerStepThrough]
    [DebuggerHidden]
    [DebuggerNonUserCode]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Task Invoke(IBehaviorContext context)
    {
        object obj = context;
        // SAFETY: Pipeline routing guarantees context is a TInContext (reference type); retype skips a redundant castclass.
        return behavior.Invoke(Unsafe.As<object, TInContext>(ref obj), next);
    }
}

static class CompletedNextCache<T> where T : IBehaviorContext
{
    public static readonly Func<T, Task> Next = _ => Task.CompletedTask;
}