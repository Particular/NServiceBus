#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

class EnvelopeComponent(EnvelopeComponent.Settings settings)
{
    public EnvelopeUnwrapper CreateUnwrapper(IServiceProvider serviceProvider)
        => new([.. settings.HandlerFactories.Select(factory => factory(serviceProvider))], serviceProvider.GetRequiredService<IncomingPipelineMetrics>());

    public class Settings
    {
        readonly Dictionary<Type, Func<IServiceProvider, IEnvelopeHandler>> factories = [];

        public void AddEnvelopeHandler<THandler>() where THandler : IEnvelopeHandler
        {
            if (factories.ContainsKey(typeof(THandler)))
            {
                return;
            }

            // create and cache the factory because the service provider is only available later
            // using CreateInstance instead of CreateFactory because the envelop handlers are instantiated only once and
            // kept alive for the lifetime of the endpoint
            factories.Add(typeof(THandler), static sp => ActivatorUtilities.CreateInstance<THandler>(sp));
        }

        public IReadOnlyCollection<Func<IServiceProvider, IEnvelopeHandler>> HandlerFactories => factories.Values;
    }
}