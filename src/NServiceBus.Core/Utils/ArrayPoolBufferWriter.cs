#nullable enable

namespace NServiceBus;

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

sealed class ArrayPoolBufferWriter<T>(
    ArrayPool<T> pool,
    int initialCapacity = ArrayPoolBufferWriter<T>.DefaultInitialBufferSize)
    : IBufferWriter<T>, IMemoryOwner<T>
{
    const int DefaultInitialBufferSize = 256;

    T[]? buffer = pool.Rent(initialCapacity);

    public ArrayPoolBufferWriter()
        : this(ArrayPool<T>.Shared)
    {
    }

    public ArrayPoolBufferWriter(int initialCapacity)
        : this(ArrayPool<T>.Shared, initialCapacity)
    {
    }

    public ReadOnlyMemory<T> WrittenMemory
    {
        get
        {
            T[]? array = buffer;

            if (array is null)
            {
                ThrowObjectDisposedException();
            }

            return array.AsMemory(0, WrittenCount);
        }
    }

    public ReadOnlySpan<T> WrittenSpan
    {
        get
        {
            T[]? array = buffer;

            if (array is null)
            {
                ThrowObjectDisposedException();
            }

            return array.AsSpan(0, WrittenCount);
        }
    }

    public int WrittenCount
    {
        get;
        private set;
    }

    public int Capacity
    {
        get
        {
            T[]? array = buffer;

            if (array is null)
            {
                ThrowObjectDisposedException();
            }

            return array.Length;
        }
    }

    public int FreeCapacity
    {
        get
        {
            T[]? array = buffer;

            if (array is null)
            {
                ThrowObjectDisposedException();
            }

            return array.Length - WrittenCount;
        }
    }

    public void Advance(int count)
    {
        T[]? array = buffer;

        if (array is null)
        {
            ThrowObjectDisposedException();
        }

        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (WrittenCount > array.Length - count)
        {
            ThrowArgumentExceptionForAdvancedTooFar();
        }

        WrittenCount += count;
    }

    public Memory<T> GetMemory(int sizeHint = 0)
    {
        CheckBufferAndEnsureCapacity(sizeHint);

        return buffer.AsMemory(WrittenCount);
    }

    public Span<T> GetSpan(int sizeHint = 0)
    {
        CheckBufferAndEnsureCapacity(sizeHint);

        return buffer.AsSpan(WrittenCount);
    }


    Memory<T> IMemoryOwner<T>.Memory => MemoryMarshal.AsMemory(WrittenMemory);

    public void Dispose()
    {
        T[]? array = buffer;

        if (array is null)
        {
            return;
        }

        buffer = null;

        pool.Return(array);
    }

    public void Clear()
    {
        T[]? array = buffer;

        if (array is null)
        {
            ThrowObjectDisposedException();
        }

        array.AsSpan(0, WrittenCount).Clear();

        WrittenCount = 0;
    }

    void CheckBufferAndEnsureCapacity(int sizeHint)
    {
        T[]? array = buffer;

        if (array is null)
        {
            ThrowObjectDisposedException();
        }

        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);

        if (sizeHint == 0)
        {
            sizeHint = 1;
        }

        if (sizeHint > array!.Length - WrittenCount)
        {
            ResizeBuffer(sizeHint);
        }
    }

    void ResizeBuffer(int sizeHint)
    {
        int minimumSize = WrittenCount + sizeHint;

        var oldArray = buffer;
        buffer = pool.Rent(minimumSize);
        oldArray.CopyTo(buffer);
        if (oldArray is not null)
        {
            pool.Return(oldArray);
        }
    }

    [DoesNotReturn]
    static void ThrowArgumentExceptionForAdvancedTooFar() => throw new ArgumentException("The buffer writer has advanced too far");

    [DoesNotReturn]
    static void ThrowObjectDisposedException() => throw new ObjectDisposedException("The current buffer has already been disposed");
}