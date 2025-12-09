namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

class EnvelopeComponent(EnvelopeComponent.Settings settings)
{
    public IEnumerable<IEnvelopeHandler> InitializeHandlers(IServiceProvider serviceProvider)
    {
        List<IEnvelopeHandler> handlers = [];
        handlers.AddRange(settings.HandlerFactories.Select(factory => (IEnvelopeHandler)factory(serviceProvider, null)));

        return handlers;
    }

    public class Settings()
    {
        readonly List<ObjectFactory> factories = [];
        public void AddEnvelopeHandler<THandler>()
        {
            //create and cache the factory
            var handlerFactory = ActivatorUtilities.CreateFactory(typeof(THandler), Type.EmptyTypes);
            factories.Add(handlerFactory);
        }

        public IEnumerable<ObjectFactory> HandlerFactories => factories;
    }
}