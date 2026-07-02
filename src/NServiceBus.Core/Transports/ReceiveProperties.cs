#nullable enable

namespace NServiceBus.Transport;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

/// <summary>
/// Properties received from the transport that can be propagated to outgoing dispatch operations.
/// Transports populate this with native message metadata that should survive audit and error operations.
/// </summary>
[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Name reflects domain semantics, not collection implementation.")]
public sealed class ReceiveProperties : IReadOnlyDictionary<string, string>
{
    /// <summary>
    /// An empty <see cref="ReceiveProperties" /> instance.
    /// </summary>
    public static ReceiveProperties Empty { get; } = new();

    /// <summary>
    /// Creates an empty instance of <see cref="ReceiveProperties" />.
    /// </summary>
    public ReceiveProperties()
    {
    }

    /// <summary>
    /// Creates a <see cref="ReceiveProperties" /> from the provided dictionary.
    /// </summary>
    public ReceiveProperties(Dictionary<string, string> dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        foreach (var kvp in dictionary)
        {
            AddInternal(kvp.Key, kvp.Value);
        }
    }

    /// <inheritdoc />
    public string this[string key]
    {
        get
        {
            for (int i = 0; i < count; i++)
            {
                ref var slot = ref slots[i];

                if (StringComparer.Ordinal.Equals(key, slot.Key))
                {
                    return slot.Value;
                }
            }

            if (stash is not null && stash.TryGetValue(key, out var value))
            {
                return value;
            }

            ThrowKeyNotFoundException(key);
            return default;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> Keys
    {
        get
        {
            for (int i = 0; i < count; i++)
            {
                yield return slots[i].Key;
            }

            if (stash is not null)
            {
                foreach (var key in stash.Keys)
                {
                    yield return key;
                }
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> Values
    {
        get
        {
            for (int i = 0; i < count; i++)
            {
                yield return slots[i].Value;
            }

            if (stash is not null)
            {
                foreach (var value in stash.Values)
                {
                    yield return value;
                }
            }
        }
    }

    /// <inheritdoc />
    public int Count => count + (stash?.Count ?? 0);

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        for (int i = 0; i < count; i++)
        {
            ref var slot = ref slots[i];

            if (StringComparer.Ordinal.Equals(key, slot.Key))
            {
                return true;
            }
        }

        return stash is not null && stash.ContainsKey(key);
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
    {
        for (int i = 0; i < count; i++)
        {
            ref var slot = ref slots[i];

            if (StringComparer.Ordinal.Equals(key, slot.Key))
            {
                value = slot.Value;
                return true;
            }
        }

        if (stash is not null && stash.TryGetValue(key, out var stashedValue))
        {
            value = stashedValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        for (int i = 0; i < count; i++)
        {
            ref var slot = ref slots[i];
            yield return new KeyValuePair<string, string>(slot.Key, slot.Value);
        }

        if (stash is not null)
        {
            foreach (var kvp in stash)
            {
                yield return kvp;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void AddInternal(string key, string value)
    {
        if (count < InlineArrayLength)
        {
            slots[count] = new Slot { Key = key, Value = value };
            count++;
            return;
        }

        (stash ??= [])[key] = value;
    }

    [DoesNotReturn]
    static void ThrowKeyNotFoundException(string key) => throw new KeyNotFoundException($"The given key '{key}' was not present in the dictionary.");

    SlotArray slots;
    int count;
    Dictionary<string, string>? stash;

    const int InlineArrayLength = 4;

    struct Slot
    {
        public string Key;
        public string Value;
    }

    [InlineArray(InlineArrayLength)]
    struct SlotArray
    {
        Slot _element0;
    }
}