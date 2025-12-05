namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

class KeyedServiceScopeFactory(IServiceScopeFactory innerFactory, object serviceKey, KeyedServiceCollectionAdapter serviceCollection) : IServiceScopeFactory
{
    public IServiceScope CreateScope()
    {
        var innerScope = innerFactory.CreateScope();
        ArgumentNullException.ThrowIfNull(innerScope);

        return new KeyedServiceScope(innerScope, serviceKey, serviceCollection);
    }

    sealed class KeyedServiceScope(
        IServiceScope innerScope,
        object serviceKey,
        KeyedServiceCollectionAdapter serviceCollection)
        : IServiceScope, IAsyncDisposable
    {
        public IServiceProvider ServiceProvider { get; } = new KeyedServiceProviderAdapter(innerScope.ServiceProvider, serviceKey, serviceCollection);

        public void Dispose() => innerScope.Dispose();

        public ValueTask DisposeAsync()
        {
            if (innerScope is IAsyncDisposable asyncDisposable)
            {
                return asyncDisposable.DisposeAsync();
            }

            innerScope.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}