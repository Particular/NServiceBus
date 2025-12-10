namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

class EnvelopeComponent(EnvelopeComponent.Settings settings)
{
    public EnvelopeUnwrapper Initialize(IServiceProvider serviceProvider)
    {
        List<IEnvelopeHandler> handlers = [];
        handlers.AddRange(settings.HandlerFactories.Select(factory => (IEnvelopeHandler)factory(serviceProvider, null)));

        return new EnvelopeUnwrapper(handlers);
    }

    public class Settings()
    {
        readonly Dictionary<Type, ObjectFactory> factories = [];
        public void AddEnvelopeHandler<THandler>()
        {
            if (factories.ContainsKey(typeof(THandler)))
            {
                return;
            }
            //create and cache the factory
            var handlerFactory = ActivatorUtilities.CreateFactory(typeof(THandler), Type.EmptyTypes);
            factories.Add(typeof(THandler), handlerFactory);
        }

        public IEnumerable<ObjectFactory> HandlerFactories => factories.Values;
    }
}