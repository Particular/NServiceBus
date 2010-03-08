using System;
using System.Linq;
using System.Collections.Generic;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Faults.Forwarder
{
   /// <summary>
   /// Configures fault forwarder.
   /// </summary>
   public static class ConfigureFaultsForwarder
   {
      /// <summary>
      /// Use the faults forwarding feature.
      /// </summary>
      /// <param name="config">Configuration object.</param>
      /// <param name="faultAggregatorEndpoint">Name of fault aggregator NSB endpoint.</param>
      /// <param name="sanitizeProcessingExceptions">If true, non-system processing exceptions will be recreated using different (system)
      /// type to avoid custom exception deserialization problem in the aggregator.</param>
      /// <returns></returns>
      public static Configure ForwardFaultsTo(this Configure config, string faultAggregatorEndpoint, bool sanitizeProcessingExceptions)
      {
         config.Configurer.ConfigureComponent<FaultManager>(ComponentCallModelEnum.Singlecall)
            .ConfigureProperty(x => x.AggregatorEndpoint, faultAggregatorEndpoint)
            .ConfigureProperty(x => x.SanitizeProcessingExceptions, sanitizeProcessingExceptions);         
         return config;
      }

      /// <summary>
      /// Use the faults forwarding feature. Non-system processing exceptions will be recreated using different (system)
      /// type to avoid custom exception deserialization problem in the aggregator
      /// </summary>
      /// <param name="config">Configuration object.</param>
      /// <param name="faultAggregatorEndpoint">Name of fault aggregator NSB endpoint.</param>      
      /// <returns></returns>
      public static Configure ForwardFaultsTo(this Configure config, string faultAggregatorEndpoint)
      {
         return ForwardFaultsTo(config, faultAggregatorEndpoint, true);
      }
   }
}