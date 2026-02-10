#nullable enable

namespace NServiceBus.Extensibility;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Pipeline;

/// <summary>
/// A string object bag of context objects.
/// </summary>
public class ContextBag : IReadOnlyContextBag
{
    /// <summary>
    /// Initialized a new instance of <see cref="ContextBag" />.
    /// </summary>
    public ContextBag(ContextBag? parentBag = null)
    {
        this.parentBag = parentBag;
        root = parentBag?.root ?? this;
        Behaviors = parentBag?.Behaviors ?? [];
        Parts = parentBag?.Parts ?? [];
        Frame = parentBag?.Frame ?? default;
    }

    /// <summary>
    /// Retrieves the specified type from the context.
    /// </summary>
    /// <typeparam name="T">The type to retrieve.</typeparam>
    /// <returns>The type instance.</returns>
    public T Get<T>() => Get<T>(typeof(T).FullName!);

    /// <summary>
    /// Tries to retrieve the specified type from the context.
    /// </summary>
    /// <typeparam name="T">The type to retrieve.</typeparam>
    /// <param name="result">The type instance.</param>
    /// <returns><code>true</code> if found, otherwise <code>false</code>.</returns>
    public bool TryGet<T>([NotNullWhen(true)] out T? result) => TryGet(typeof(T).FullName!, out result);

    /// <summary>
    /// Tries to retrieve the specified type from the context.
    /// </summary>
    /// <typeparam name="T">The type to retrieve.</typeparam>
    /// <param name="key">The key of the value being looked up.</param>
    /// <param name="result">The type instance.</param>
    /// <returns><code>true</code> if found, otherwise <code>false</code>.</returns>
    public bool TryGet<T>(string key, [NotNullWhen(true)] out T? result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (stash?.TryGetValue(key, out var value) == true)
        {
            result = (T)value;
            return true;
        }

        if (parentBag != null)
        {
            return parentBag.TryGet(key, out result);
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public T Get<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!TryGet(key, out T? value))
        {
            ThrowKeyNotFoundException(key);
        }

        return value;

        [DoesNotReturn]
        static void ThrowKeyNotFoundException(string key) => throw new KeyNotFoundException($"No item found in behavior context with key: {key}");
    }

    /// <summary>
    /// Gets the requested extension, a new one will be created if needed.
    /// </summary>
    public T GetOrCreate<T>() where T : class, new()
    {
        if (TryGet(out T? value))
        {
            return value;
        }

        var newInstance = new T();
        Set(newInstance);
        return newInstance;
    }


    /// <summary>
    /// Stores the type instance in the context.
    /// </summary>
    /// <typeparam name="T">The type to store.</typeparam>
    /// <param name="t">The instance type to store.</param>
    public void Set<T>(T t) => Set(typeof(T).FullName!, t);


    /// <summary>
    /// Removes the instance type from the context.
    /// </summary>
    /// <typeparam name="T">The type to remove.</typeparam>
    public void Remove<T>() => Remove(typeof(T).FullName!);

    /// <summary>
    /// Removes the instance type from the context.
    /// </summary>
    /// <param name="key">The key of the value being removed.</param>
    public void Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _ = stash?.Remove(key);
    }

    /// <summary>
    /// Stores the passed instance in the context.
    /// </summary>
    public void Set<T>(string key, T t)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(t);

        GetOrCreateStash()[key] = t;
    }

    /// <summary>
    /// Sets a value on the context bag at the root of the context chain.
    /// This can enable sharing context across the main and the recoverability pipeline or across forks without an existing value holder present in the shared context hierarchy
    ///
    /// Be careful, values set on the root are available to all pipeline forks that are created off the root context! Therefore there there's a risk of conflicting keys or overriding existing keys from other forks. The same pipeline behaviors can be executed multiple times on nested chains (e.g. nested sends).
    /// 
    /// </summary>
    internal void SetOnRoot<T>(string key, T t)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(t);

        root.Set(key, t);
    }

    /// <summary>
    /// Merges the passed context into this one.
    /// </summary>
    /// <param name="context">The source context.</param>
    internal void Merge(ContextBag context)
    {
        if (context.stash == null)
        {
            return;
        }

        var targetStash = GetOrCreateStash();
        foreach (var kvp in context.stash)
        {
            targetStash[kvp.Key] = kvp.Value;
        }
    }

    Dictionary<string, object> GetOrCreateStash()
    {
        stash ??= [];

        return stash;
    }

    /// <summary>
    /// This internal property is here for performance optimizations. It allows the pipeline to set all
    /// behaviors of a given stage which then can be extracted as part of the next delegate invocation from the context
    /// to avoid closure capturing.
    /// </summary>
    internal IBehavior[] Behaviors { get; set; }

    internal PipelinePart[] Parts { get; set; }

    internal PipelineFrame Frame;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal IBehavior GetBehavior() =>
        Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Behaviors), Frame.Index);

    internal ContextBag? parentBag;

    private protected ContextBag root;

    Dictionary<string, object>? stash;
}