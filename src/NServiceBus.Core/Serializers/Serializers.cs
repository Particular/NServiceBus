namespace NServiceBus.Features.Categories
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Serialization;

    /// <summary>
    /// Category for all serializers
    /// </summary>
    public class Serializers : FeatureCategory
    {
        public override IEnumerable<Feature> GetFeaturesToInitialize()
        {
            //has the users already registered his own serializer? (mostly for backwards compatibility)
            if (Configure.Instance.Configurer.HasComponent<IMessageSerializer>())
                yield break;

            var availableSerializers = GetAllAvailableFeatures();

            var enabledSerializers = availableSerializers.Where(f => f.Enabled).ToList();

            if (enabledSerializers.Count() > 1)
                throw new ConfigurationErrorsException("Multiple serializers are not supported. Please make sure to only enable one");

            //default to xml
            var serializerToUse = availableSerializers.Single(f => f.GetType() == typeof (XmlSerialization));

            if (enabledSerializers.Any())
                serializerToUse = availableSerializers.Single();

            yield return serializerToUse;
        }
    }
}