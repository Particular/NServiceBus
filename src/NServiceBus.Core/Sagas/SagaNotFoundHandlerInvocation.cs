#nullable enable
namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

sealed class SagaNotFoundHandlerInvocation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSagaNotFoundHandler> : ISagaNotFoundHandlerInvocation where TSagaNotFoundHandler : ISagaNotFoundHandler
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
            if (notFoundHandler is IAsyncDisposable asyncDisposableHandler)
            {
                await asyncDisposableHandler.DisposeAsync().ConfigureAwait(false);
            }
            else if (notFoundHandler is IDisposable disposableHandler)
            {
                disposableHandler.Dispose();
            }
        }
    }

    static readonly ObjectFactory<TSagaNotFoundHandler> factory = ActivatorUtilities.CreateFactory<TSagaNotFoundHandler>([]);
}