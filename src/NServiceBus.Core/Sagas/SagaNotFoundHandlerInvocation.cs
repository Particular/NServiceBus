#nullable enable
namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

class SagaNotFoundHandlerInvocation<TSagaNotFoundHandler> : ISagaNotFoundHandlerInvocation where TSagaNotFoundHandler : NServiceBus.IHandleSagaNotFound
{
    public async Task Invoke(IServiceProvider serviceProvider, object message, IMessageProcessingContext context)
    {
        var notFoundHandler = factory(serviceProvider, []);

        try
        {
            await notFoundHandler.Handle(message, context).ConfigureAwait(false);
        }
        finally
        {
            //TODO add tests for disposable
            if (notFoundHandler is IAsyncDisposable asyncDisposableInstaller)
            {
                await asyncDisposableInstaller.DisposeAsync().ConfigureAwait(false);
            }
            else if (notFoundHandler is IDisposable disposableInstaller)
            {
                disposableInstaller.Dispose();
            }
        }
    }

    static readonly ObjectFactory<TSagaNotFoundHandler> factory = ActivatorUtilities.CreateFactory<TSagaNotFoundHandler>([]);
}