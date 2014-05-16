namespace NServiceBus.Features
{
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Settings for the various features
    /// </summary>
    public class FeatureSettings:IEnumerable<Feature>
    {
        /// <summary>
        /// Enables the given feature
        /// </summary>
        public FeatureSettings Enable<T>() where T : Feature
        {
            Feature.Enable<T>();

            return this;
        }

        /// <summary>
        /// Disables the given feature
        /// </summary>
        public FeatureSettings Disable<T>() where T : Feature
        {
            Feature.Disable<T>();

            return this;
        }

        public void Add(Feature feature)
        {
            if (feature.IsEnabledByDefault)
            {
                Feature.EnableByDefault(feature.GetType());    
            }
            
            features.Add(feature);
        }

        List<Feature> features = new List<Feature>();
        public IEnumerator<Feature> GetEnumerator()
        {
            return features.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void EnableByDefault<T>() where T:Feature
        {
            Feature.EnableByDefault<T>();    
        }
    }
}