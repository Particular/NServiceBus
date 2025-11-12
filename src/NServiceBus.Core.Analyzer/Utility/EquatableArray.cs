namespace NServiceBus.Core.Analyzer;

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

/// <summary>
/// Useful in incremental source generators because ImmutableArray&lt;T&gt;> is not memoizable.
/// </summary>
readonly record struct EquatableArray<T>(ImmutableArray<T> Items)
{
    public bool Equals(EquatableArray<T> other) =>
        Items.SequenceEqual(other.Items);

    public override int GetHashCode()
    {
        // System.HashCode unavailable in netstandard2.0 :-(

        unchecked // Overflow is fine, just wrap
        {
            int hash = 17;
            foreach (var item in Items)
            {
                hash = (hash * 23) + item.GetHashCode();
            }
            return hash;
        }
    }

    public static implicit operator ImmutableArray<T>(EquatableArray<T> e) => e.Items;
    public static implicit operator EquatableArray<T>(ImmutableArray<T> a) => new(a);

    public override string ToString()
    {
        var b = new StringBuilder("[ ");
        var i = 0;
        foreach (var item in Items)
        {
            if (i++ > 0)
            {
                b.Append(", ");
            }
            _ = b.Append(item);
        }

        b.Append(" ] ");
        return b.ToString();
    }
}