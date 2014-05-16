namespace NServiceBus.Features.Categories
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Serialization;

    /// <summary>
    /// Category for all serializers
    /// </summary>
    public class Serializers : FeatureCategory
    {
        //default to xml
        static Type DefaultSerializer = typeof(XmlSerialization);

        public override IEnumerable<Feature> GetFeaturesToInitialize()
        {
            //has the users already registered his own serializer? (mostly for backwards compatibility)
            if (Configure.Instance.Configurer.HasComponent<IMessageSerializer>())
                yield break;

            var availableSerializers = GetAllAvailableFeatures().ToList();

            var enabledSerializers = availableSerializers.Where(f => f.Enabled).ToList();

            if (enabledSerializers.Count() > 1)
                throw new ConfigurationErrorsException("Multiple serializers are not supported. Please make sure to only enable one");

            var serializerToUse = availableSerializers.Single(f => f.GetType() == DefaultSerializer);

            if (enabledSerializers.Any())
                serializerToUse = enabledSerializers.Single();

            yield return serializerToUse;
        }

        public static void SetDefault<T>() where T:Feature
        {
            DefaultSerializer = typeof(T);
        }
    }
}