#nullable enable

namespace NServiceBus;

using System;
using System.Threading;

class EndpointInstanceAccessor
{
    public IEndpointInstance Get()
    {
        var current = Volatile.Read(ref instance);
        if (current == null)
        {
            throw new InvalidOperationException("The endpoint instance is only available after the endpoint has started.");
        }

        return current;
    }

    public void Set(IEndpointInstance endpointInstance)
    {
        ArgumentNullException.ThrowIfNull(endpointInstance);

        if (Interlocked.CompareExchange(ref instance, endpointInstance, null) != null)
        {
            throw new InvalidOperationException("The endpoint instance has already been set.");
        }
    }

    IEndpointInstance? instance;
}
