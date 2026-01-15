#nullable enable
namespace NServiceBus;

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

sealed class LazyArrayPoolBufferWriter<T>(int? capacity = null) : IBufferWriter<T>, IMemoryOwner<T>
{
    ArrayPoolBufferWriter<T>? innerWriter;

    public ReadOnlyMemory<T> WrittenMemory => innerWriter?.WrittenMemory ?? ReadOnlyMemory<T>.Empty;

    public ReadOnlySpan<T> WrittenSpan => innerWriter is not null ? innerWriter.WrittenSpan : [];

    public int WrittenCount => innerWriter?.WrittenCount ?? 0;

    public int Capacity
    {
        get
        {
            EnsureInnerWriter();
            return innerWriter.Capacity;
        }
    }

    public int FreeCapacity
    {
        get
        {
            EnsureInnerWriter();
            return innerWriter.FreeCapacity;
        }
    }

    public void Advance(int count)
    {
        EnsureInnerWriter();
        innerWriter.Advance(count);
    }

    public Memory<T> GetMemory(int sizeHint = 0)
    {
        EnsureInnerWriter();
        return innerWriter.GetMemory(sizeHint);
    }

    public Span<T> GetSpan(int sizeHint = 0)
    {
        EnsureInnerWriter();
        return innerWriter.GetSpan(sizeHint);
    }

    public void Clear() => innerWriter?.Clear();
    public void Dispose() => innerWriter?.Dispose();

    Memory<T> IMemoryOwner<T>.Memory => MemoryMarshal.AsMemory(WrittenMemory);

    [MemberNotNull(nameof(innerWriter))]
    void EnsureInnerWriter() => innerWriter ??= capacity is null ? new ArrayPoolBufferWriter<T>() : new ArrayPoolBufferWriter<T>(capacity.Value);
}