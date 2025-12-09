namespace NServiceBus;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

class EnvelopeComponent(EnvelopeComponent.Settings settings)
{
    public IEnumerable<IEnvelopeHandler> InitializeHandlers(IServiceProvider serviceProvider)
    {
        foreach (ObjectFactory factory in settings.HandlerFactories)
        {
            yield return (IEnvelopeHandler)factory(serviceProvider, null);
        }
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