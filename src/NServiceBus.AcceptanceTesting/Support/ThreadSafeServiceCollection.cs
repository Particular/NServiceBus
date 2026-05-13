namespace NServiceBus.AcceptanceTesting.Support;

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

sealed class ThreadSafeServiceCollection : IServiceCollection
{
    public int Count
    {
        get
        {
            using var _ = gate.EnterScope();
            return inner.Count;
        }
    }

    public bool IsReadOnly => inner.IsReadOnly;

    public ServiceDescriptor this[int index]
    {
        get
        {
            using var _ = gate.EnterScope();
            return inner[index];
        }
        set
        {
            using var _ = gate.EnterScope();
            inner[index] = value;
        }
    }

    public void Add(ServiceDescriptor item)
    {
        using var _ = gate.EnterScope();
        inner.Add(item);
    }

    public void Clear()
    {
        using var _ = gate.EnterScope();
        inner.Clear();
    }

    public bool Contains(ServiceDescriptor item)
    {
        using var _ = gate.EnterScope();
        return inner.Contains(item);
    }

    public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
    {
        using var _ = gate.EnterScope();
        inner.CopyTo(array, arrayIndex);
    }

    public bool Remove(ServiceDescriptor item)
    {
        using var _ = gate.EnterScope();
        return inner.Remove(item);
    }

    public IEnumerator<ServiceDescriptor> GetEnumerator()
    {
        using var _ = gate.EnterScope();
        IEnumerable<ServiceDescriptor> snapshot = [.. inner];
        return snapshot.GetEnumerator();
    }

    public int IndexOf(ServiceDescriptor item)
    {
        using var _ = gate.EnterScope();
        return inner.IndexOf(item);
    }

    public void Insert(int index, ServiceDescriptor item)
    {
        using var _ = gate.EnterScope();
        inner.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        using var _ = gate.EnterScope();
        inner.RemoveAt(index);
    }

    internal IServiceCollection Unwrap() => inner;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    readonly ServiceCollection inner = [];
    readonly Lock gate = new();
}