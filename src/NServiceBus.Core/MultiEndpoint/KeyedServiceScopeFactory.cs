#nullable enable
namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

class KeyedServiceScopeFactory(IServiceScopeFactory innerFactory, object serviceKey, KeyedServiceCollectionAdapter serviceCollection) : IServiceScopeFactory
{
    public IServiceScope CreateScope()
    {
        var innerScope = innerFactory.CreateScope();
        return new KeyedServiceScope(innerScope, serviceKey, serviceCollection);
    }

    class KeyedServiceScope : IServiceScope, IAsyncDisposable
    {
        public KeyedServiceScope(IServiceScope innerScope, object serviceKey, KeyedServiceCollectionAdapter serviceCollection)
        {
            ArgumentNullException.ThrowIfNull(innerScope);

            this.innerScope = innerScope;
            ServiceProvider = new KeyedServiceProviderAdapter(innerScope.ServiceProvider, serviceKey, serviceCollection);
        }

        public IServiceProvider ServiceProvider { get; }

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

        readonly IServiceScope innerScope;
    }
}