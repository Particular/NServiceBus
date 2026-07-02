#nullable enable

namespace NServiceBus.Transport;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DelayedDelivery;
using Performance.TimeToBeReceived;

/// <summary>
/// Describes additional properties for an outgoing message.
/// </summary>
[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Name reflects domain semantics, not collection implementation.")]
public class DispatchProperties : IDictionary<string, string>
{
    //These can't be changed to be backwards compatible with previous versions of the core
    static readonly string DoNotDeliverBeforeKeyName = "DeliverAt";
    static readonly string DelayDeliveryWithKeyName = "DelayDeliveryFor";
    static readonly string DiscardIfNotReceivedBeforeKeyName = "TimeToBeReceived";

    /// <summary>
    /// Creates a new instance of <see cref="DispatchProperties"/>.
    /// </summary>
    public DispatchProperties()
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="DispatchProperties"/> an copies the values from the provided dictionary.
    /// </summary>
    public DispatchProperties(Dictionary<string, string> properties)
    {
        if (properties is null)
        {
            return;
        }

        foreach (var kvp in properties)
        {
            AddInternal(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="DispatchProperties"/> an copies the values from the provided dictionary.
    /// </summary>
    public DispatchProperties(IDictionary<string, string> properties)
    {
        if (properties is null)
        {
            return;
        }

        foreach (var kvp in properties)
        {
            AddInternal(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Delay message delivery to a specific <see cref="DateTimeOffset"/>.
    /// </summary>
    public DoNotDeliverBefore? DoNotDeliverBefore
    {
        get => ContainsKey(DoNotDeliverBeforeKeyName)
            ? new DoNotDeliverBefore(DateTimeOffsetHelper.ToDateTimeOffset(this[DoNotDeliverBeforeKeyName]))
            : null;

        set => this[DoNotDeliverBeforeKeyName] = DateTimeOffsetHelper.ToWireFormattedString(value!.At);
    }

    /// <summary>
    /// Delay message delivery by a certain <see cref="TimeSpan"/>.
    /// </summary>
    public DelayDeliveryWith? DelayDeliveryWith
    {
        get => ContainsKey(DelayDeliveryWithKeyName)
            ? new DelayDeliveryWith(TimeSpan.Parse(this[DelayDeliveryWithKeyName]))
            : null;

        set => this[DelayDeliveryWithKeyName] = value!.Delay.ToString();
    }

    /// <summary>
    /// Discard the message after a certain period of time.
    /// </summary>
    public DiscardIfNotReceivedBefore? DiscardIfNotReceivedBefore
    {
        get => ContainsKey(DiscardIfNotReceivedBeforeKeyName)
            ? new DiscardIfNotReceivedBefore(TimeSpan.Parse(this[DiscardIfNotReceivedBeforeKeyName]))
            : null;

        set => this[DiscardIfNotReceivedBeforeKeyName] = value!.MaxTime.ToString();
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
        set
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

            if (count < InlineArrayLength)
            {
                slots[count] = new Slot { Key = key, Value = value };
                count++;
                return;
            }

            (stash ??= [])[key] = value;
        }
    }

    /// <inheritdoc />
    public ICollection<string> Keys
    {
        get
        {
            var keys = new string[count + (stash?.Count ?? 0)];
            int index = 0;

            for (int i = 0; i < count; i++)
            {
                keys[index++] = slots[i].Key;
            }

            if (stash is not null)
            {
                foreach (var key in stash.Keys)
                {
                    keys[index++] = key;
                }
            }

            return keys;
        }
    }

    /// <inheritdoc />
    public ICollection<string> Values
    {
        get
        {
            var values = new string[count + (stash?.Count ?? 0)];
            int index = 0;

            for (int i = 0; i < count; i++)
            {
                values[index++] = slots[i].Value;
            }

            if (stash is not null)
            {
                foreach (var value in stash.Values)
                {
                    values[index++] = value;
                }
            }

            return values;
        }
    }

    /// <inheritdoc />
    public int Count => count + (stash?.Count ?? 0);

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public void Add(string key, string value)
    {
        for (int i = 0; i < count; i++)
        {
            ref var slot = ref slots[i];

            if (StringComparer.Ordinal.Equals(key, slot.Key))
            {
                throw new ArgumentException($"An item with the same key '{key}' has already been added.");
            }
        }

        if (stash is not null && stash.ContainsKey(key))
        {
            throw new ArgumentException($"An item with the same key '{key}' has already been added.");
        }

        AddInternal(key, value);
    }

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
    public bool Remove(string key)
    {
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

                slots[count] = new Slot();
                return true;
            }
        }

        return stash?.Remove(key) ?? false;
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, out string value)
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

        value = default!;
        return false;
    }

    /// <summary>
    /// Attempts to add the specified key and value to the dictionary.
    /// </summary>
    /// <returns>true if the key/value pair was added; false if the key already exists.</returns>
    public bool TryAdd(string key, string value)
    {
        for (int i = 0; i < count; i++)
        {
            ref var slot = ref slots[i];

            if (StringComparer.Ordinal.Equals(key, slot.Key))
            {
                return false;
            }
        }

        if (stash is not null && stash.ContainsKey(key))
        {
            return false;
        }

        AddInternal(key, value);
        return true;
    }

    /// <inheritdoc />
    public void Clear()
    {
        for (int i = 0; i < count; i++)
        {
            slots[i] = default;
        }

        count = 0;
        stash?.Clear();
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

    void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item) => Add(item.Key, item.Value);

    bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item)
    {
        return TryGetValue(item.Key, out var value) && StringComparer.Ordinal.Equals(value, item.Value);
    }

    void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

        if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentException("Destination array is not long enough to copy all items.");
        }

        int index = arrayIndex;

        for (int i = 0; i < count; i++)
        {
            ref var slot = ref slots[i];
            array[index++] = new KeyValuePair<string, string>(slot.Key, slot.Value);
        }

        if (stash is not null)
        {
            foreach (var kvp in stash)
            {
                array[index++] = kvp;
            }
        }
    }

    bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
    {
        if (!((ICollection<KeyValuePair<string, string>>)this).Contains(item))
        {
            return false;
        }

        return Remove(item.Key);
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