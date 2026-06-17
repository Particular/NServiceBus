#nullable enable

namespace NServiceBus.Extensibility;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
        Invoker = parentBag?.Invoker ?? (static _ => Task.CompletedTask);
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

        for (int i = 0; i < count; i++)
        {
            ref var slot = ref slots[i];

            if (StringComparer.Ordinal.Equals(key, slot.Key))
            {
                result = (T)slot.Value!;
                return true;
            }
        }

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

        for (int i = 0; i < count; i++)
        {
            ref var slot = ref slots[i];

            if (StringComparer.Ordinal.Equals(key, slot.Key))
            {
                count--;

                if (i != count)
                {
                    slots[i] = slots[count];
                }

                slots[count] = default;
                return;
            }
        }

        _ = stash?.Remove(key);
    }

    /// <summary>
    /// Stores the passed instance in the context.
    /// </summary>
    public void Set<T>(string key, T t)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(t);

        for (int i = 0; i < count; i++)
        {
            ref var slot = ref slots[i];

            if (StringComparer.Ordinal.Equals(key, slot.Key))
            {
                slot.Value = t;
                return;
            }
        }

        if (count < 8)
        {
            slots[count] = new Slot { Key = key, Value = t };
            count++;
            return;
        }

        var s = stash;
        if (s is null)
        {
            s = [];
            stash = s;
        }

        s[key] = t;
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
        if (count == 0 && stash is null && context.stash is null)
        {
            var sourceCount = context.count;

            for (int i = 0; i < sourceCount; i++)
            {
                slots[i] = context.slots[i];
            }

            count = sourceCount;
            return;
        }

        for (int i = 0; i < context.count; i++)
        {
            ref var sourceSlot = ref context.slots[i];
            SetInlineOrStash(sourceSlot.Key!, sourceSlot.Value!);
        }

        var sourceStash = context.stash;
        if (sourceStash is null)
        {
            return;
        }

        if (count == 8)
        {
            var targetStash = stash ??= [];

            foreach (var kvp in sourceStash)
            {
                targetStash[kvp.Key] = kvp.Value;
            }

            return;
        }

        foreach (var kvp in sourceStash)
        {
            SetInlineOrStash(kvp.Key, kvp.Value);
        }

        return;

        void SetInlineOrStash(string key, object value)
        {
            for (int i = 0; i < count; i++)
            {
                ref var slot = ref slots[i];

                if (StringComparer.Ordinal.Equals(key, slot.Key))
                {
                    slot.Value = value;
                    return;
                }
            }

            if (count < 8)
            {
                slots[count] = new Slot
                {
                    Key = key,
                    Value = value
                };
                count++;
                return;
            }

            (stash ??= [])[key] = value;
        }
    }

    /// <summary>
    /// Removes all entries from the context.
    /// </summary>
    internal void Clear()
    {
        for (int i = 0; i < count; i++)
        {
            slots[i] = default;
        }

        count = 0;
        stash?.Clear();
    }

    internal Func<IBehaviorContext, Task> Invoker { get; set; }

    internal ContextBag? parentBag;

    private protected ContextBag root;

    SlotArray slots;
    int count;
    Dictionary<string, object>? stash;

    struct Slot
    {
        public string? Key;
        public object? Value;
    }

    [InlineArray(8)]
    struct SlotArray
    {
        Slot _element0;
    }
}