namespace NServiceBus;

using System;

sealed class ThrowingServiceProvider : IServiceProvider
{
    public object GetService(Type serviceType) => throw new NotImplementedException("This is a fake service provider that does not support resolving services.");
}