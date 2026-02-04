#nullable enable

namespace NServiceBus.Core.Analyzer;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
///     Provides an immutable list implementation which implements sequence equality.
/// </summary>
[DebuggerDisplay("Values = {_values}, Count = {Count}")]
public sealed class ImmutableEquatableArray<T>(IEnumerable<T> values)
    : IEquatable<ImmutableEquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    readonly T[] _values = [.. values];

    public static ImmutableEquatableArray<T> Empty { get; } = new([]);

    public bool Equals(ImmutableEquatableArray<T>? other)
        => other != null && ((ReadOnlySpan<T>)_values).SequenceEqual(other._values);

    public T this[int index] => _values[index];
    public int Count => _values.Length;
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

    public override bool Equals(object? obj)
        => obj is ImmutableEquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        // System.HashCode unavailable in netstandard2.0 :-(
        unchecked // Overflow is fine, just wrap
        {
            int hash = 17;
            foreach (var value in _values)
            {
                hash = (hash * 23) + (value is null ? 0 : value.GetHashCode());
            }

            return hash;
        }
    }

    public Enumerator GetEnumerator() => new(_values);

    public struct Enumerator
    {
        readonly T[] _values;
        int _index;

        internal Enumerator(T[] values)
        {
            _values = values;
            _index = -1;
        }

        public bool MoveNext()
        {
            int newIndex = _index + 1;

            if ((uint)newIndex < (uint)_values.Length)
            {
                _index = newIndex;
                return true;
            }

            return false;
        }

        public readonly T Current => _values[_index];
    }
}

static class ImmutableEquatableArray
{
    public static ImmutableEquatableArray<T> ToImmutableEquatableArray<T>(this IEnumerable<T> values)
        where T : IEquatable<T>
        => new(values);
}