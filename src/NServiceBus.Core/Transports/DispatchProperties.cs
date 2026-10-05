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
public class DispatchProperties : IDictionary<string, string>, IReadOnlyDictionary<string, string>
{
    //These can't be changed to be backwards compatible with previous versions of the core
    const string DoNotDeliverBeforeKeyName = "DeliverAt";
    const string DelayDeliveryWithKeyName = "DelayDeliveryFor";
    const string DiscardIfNotReceivedBeforeKeyName = "TimeToBeReceived";

    // Dedicated fields for the three well-known properties, avoiding string-key lookups
    string? deliverAt;
    string? delayDeliveryFor;
    string? timeToBeReceived;

    /// <summary>
    /// Creates a new instance of <see cref="DispatchProperties"/>.
    /// </summary>
    public DispatchProperties()
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="DispatchProperties"/> and copies the values from the provided dictionary.
    /// </summary>
    public DispatchProperties(Dictionary<string, string> properties)
        : this((IDictionary<string, string>)properties)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="DispatchProperties"/> and copies the values from the provided dictionary.
    /// </summary>
    public DispatchProperties(IDictionary<string, string> properties)
    {
        if (properties is null)
        {
            return;
        }

        if (properties is DispatchProperties source)
        {
            // Fast path: direct field copy when source is a DispatchProperties
            deliverAt = source.deliverAt;
            delayDeliveryFor = source.delayDeliveryFor;
            timeToBeReceived = source.timeToBeReceived;

            count = source.count;
            for (int i = 0; i < count; i++)
            {
                slots[i] = source.slots[i];
            }

            if (source.stash is { Count: > 0 })
            {
                stash = new Dictionary<string, string>(source.stash);
            }

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
        get => deliverAt is not null
            ? new DoNotDeliverBefore(DateTimeOffsetHelper.ToDateTimeOffset(deliverAt))
            : null;

        set => deliverAt = value is not null ? DateTimeOffsetHelper.ToWireFormattedString(value.At) : null;
    }

    /// <summary>
    /// Delay message delivery by a certain <see cref="TimeSpan"/>.
    /// </summary>
    public DelayDeliveryWith? DelayDeliveryWith
    {
        get => delayDeliveryFor is not null
            ? new DelayDeliveryWith(TimeSpan.Parse(delayDeliveryFor))
            : null;

        set => delayDeliveryFor = value?.Delay.ToString();
    }

    /// <summary>
    /// Discard the message after a certain period of time.
    /// </summary>
    public DiscardIfNotReceivedBefore? DiscardIfNotReceivedBefore
    {
        get => timeToBeReceived is not null
            ? new DiscardIfNotReceivedBefore(TimeSpan.Parse(timeToBeReceived))
            : null;

        set => timeToBeReceived = value?.MaxTime.ToString();
    }

    /// <inheritdoc />
    public string this[string key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);
            if (TryGetValueFromFields(key, out var fieldValue))
            {
                return fieldValue;
            }

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
            return null;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(key);
            if (TrySetField(key, value))
            {
                return;
            }

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
            var keys = new string[Count];
            int index = 0;

            if (deliverAt is not null)
            {
                keys[index++] = DoNotDeliverBeforeKeyName;
            }

            if (delayDeliveryFor is not null)
            {
                keys[index++] = DelayDeliveryWithKeyName;
            }

            if (timeToBeReceived is not null)
            {
                keys[index++] = DiscardIfNotReceivedBeforeKeyName;
            }

            for (int i = 0; i < count; i++)
            {
                keys[index++] = slots[i].Key;
            }

            if (stash is null)
            {
                return keys;
            }

            foreach (var key in stash.Keys)
            {
                keys[index++] = key;
            }

            return keys;
        }
    }

    /// <inheritdoc />
    public ICollection<string> Values
    {
        get
        {
            var values = new string[Count];
            int index = 0;

            if (deliverAt is not null)
            {
                values[index++] = deliverAt;
            }

            if (delayDeliveryFor is not null)
            {
                values[index++] = delayDeliveryFor;
            }

            if (timeToBeReceived is not null)
            {
                values[index++] = timeToBeReceived;
            }

            for (int i = 0; i < count; i++)
            {
                values[index++] = slots[i].Value;
            }

            if (stash is null)
            {
                return values;
            }

            foreach (var value in stash.Values)
            {
                values[index++] = value;
            }

            return values;
        }
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            int fieldCount = 0;

            if (deliverAt is not null)
            {
                fieldCount++;
            }

            if (delayDeliveryFor is not null)
            {
                fieldCount++;
            }

            if (timeToBeReceived is not null)
            {
                fieldCount++;
            }

            return fieldCount + count + (stash?.Count ?? 0);
        }
    }

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public void Add(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (ContainsKeyInFields(key))
        {
            throw new ArgumentException($"An item with the same key '{key}' has already been added.");
        }

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
        ArgumentNullException.ThrowIfNull(key);
        if (ContainsKeyInFields(key))
        {
            return true;
        }

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
        ArgumentNullException.ThrowIfNull(key);
        if (RemoveFromFields(key))
        {
            return true;
        }

        for (int i = 0; i < count; i++)
        {
            ref var slot = ref slots[i];

            if (!StringComparer.Ordinal.Equals(key, slot.Key))
            {
                continue;
            }

            count--;
            if (i != count)
            {
                slots[i] = slots[count];
            }

            slots[count] = new Slot();
            return true;
        }

        return stash?.Remove(key) ?? false;
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (TryGetValueFromFields(key, out var fieldValue))
        {
            value = fieldValue;
            return true;
        }

        for (int i = 0; i < count; i++)
        {
            ref var slot = ref slots[i];

            if (!StringComparer.Ordinal.Equals(key, slot.Key))
            {
                continue;
            }

            value = slot.Value;
            return true;
        }

        if (stash is not null && stash.TryGetValue(key, out var stashedValue))
        {
            value = stashedValue;
            return true;
        }

        value = null!;
        return false;
    }

    /// <summary>
    /// Attempts to add the specified key and value to the dictionary.
    /// </summary>
    /// <returns>true if the key/value pair was added; false if the key already exists.</returns>
    public bool TryAdd(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (ContainsKeyInFields(key))
        {
            return false;
        }

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
        deliverAt = null;
        delayDeliveryFor = null;
        timeToBeReceived = null;

        for (int i = 0; i < count; i++)
        {
            slots[i] = new Slot();
        }

        count = 0;
        stash?.Clear();
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        if (deliverAt is not null)
        {
            yield return new KeyValuePair<string, string>(DoNotDeliverBeforeKeyName, deliverAt);
        }

        if (delayDeliveryFor is not null)
        {
            yield return new KeyValuePair<string, string>(DelayDeliveryWithKeyName, delayDeliveryFor);
        }

        if (timeToBeReceived is not null)
        {
            yield return new KeyValuePair<string, string>(DiscardIfNotReceivedBeforeKeyName, timeToBeReceived);
        }

        for (int i = 0; i < count; i++)
        {
            ref var slot = ref slots[i];
            yield return new KeyValuePair<string, string>(slot.Key, slot.Value);
        }

        if (stash is null)
        {
            yield break;
        }

        foreach (var kvp in stash)
        {
            yield return kvp;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    bool ContainsKeyInFields(string key)
    {
        if (deliverAt is not null && key == DoNotDeliverBeforeKeyName)
        {
            return true;
        }

        if (delayDeliveryFor is not null && key == DelayDeliveryWithKeyName)
        {
            return true;
        }

        return timeToBeReceived is not null && key == DiscardIfNotReceivedBeforeKeyName;
    }

    bool TryGetValueFromFields(string key, out string value)
    {
        if (deliverAt is not null && key == DoNotDeliverBeforeKeyName)
        {
            value = deliverAt;
            return true;
        }

        if (delayDeliveryFor is not null && key == DelayDeliveryWithKeyName)
        {
            value = delayDeliveryFor;
            return true;
        }

        if (timeToBeReceived is not null && key == DiscardIfNotReceivedBeforeKeyName)
        {
            value = timeToBeReceived;
            return true;
        }

        value = null!;
        return false;
    }

    bool TrySetField(string key, string value)
    {
        switch (key)
        {
            case DoNotDeliverBeforeKeyName:
                deliverAt = value;
                return true;
            case DelayDeliveryWithKeyName:
                delayDeliveryFor = value;
                return true;
            case DiscardIfNotReceivedBeforeKeyName:
                timeToBeReceived = value;
                return true;
            default:
                return false;
        }
    }

    bool RemoveFromFields(string key)
    {
        if (deliverAt is not null && key == DoNotDeliverBeforeKeyName)
        {
            deliverAt = null;
            return true;
        }

        if (delayDeliveryFor is not null && key == DelayDeliveryWithKeyName)
        {
            delayDeliveryFor = null;
            return true;
        }

        if (timeToBeReceived is null || key != DiscardIfNotReceivedBeforeKeyName)
        {
            return false;
        }

        timeToBeReceived = null;
        return true;

    }

    void AddInternal(string key, string value)
    {
        if (TrySetField(key, value))
        {
            return;
        }

        if (count < InlineArrayLength)
        {
            slots[count] = new Slot { Key = key, Value = value };
            count++;
            return;
        }

        (stash ??= [])[key] = value;
    }

    IEnumerable<string> IReadOnlyDictionary<string, string>.Keys => Keys;
    IEnumerable<string> IReadOnlyDictionary<string, string>.Values => Values;

    void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item) => Add(item.Key, item.Value);

    bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item) => TryGetValue(item.Key, out var value) && StringComparer.Ordinal.Equals(value, item.Value);

    void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

        if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentException("Destination array is not long enough to copy all items.");
        }

        int index = arrayIndex;

        if (deliverAt is not null)
        {
            array[index++] = new KeyValuePair<string, string>(DoNotDeliverBeforeKeyName, deliverAt);
        }

        if (delayDeliveryFor is not null)
        {
            array[index++] = new KeyValuePair<string, string>(DelayDeliveryWithKeyName, delayDeliveryFor);
        }

        if (timeToBeReceived is not null)
        {
            array[index++] = new KeyValuePair<string, string>(DiscardIfNotReceivedBeforeKeyName, timeToBeReceived);
        }

        for (int i = 0; i < count; i++)
        {
            ref var slot = ref slots[i];
            array[index++] = new KeyValuePair<string, string>(slot.Key, slot.Value);
        }

        if (stash is null)
        {
            return;
        }

        foreach (var kvp in stash)
        {
            array[index++] = kvp;
        }
    }

    bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item) => ((ICollection<KeyValuePair<string, string>>)this).Contains(item) && Remove(item.Key);

    [DoesNotReturn]
    static void ThrowKeyNotFoundException(string key) => throw new KeyNotFoundException($"The given key '{key}' was not present in the dictionary.");

    SlotArray slots;
    int count;
    Dictionary<string, string>? stash;

    // Only needed for custom properties added by transports; the three well-known
    // properties are stored as dedicated fields to avoid string-key lookups.
    const int InlineArrayLength = 2;

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