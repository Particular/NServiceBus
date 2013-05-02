namespace NServiceBus.Features
{
    using System;

    public static class FeatureExtensions
    {
        public static string FeatureName(this Type featureType)
        {
            return featureType.Name.Replace("Feature", "");
        }
    }
}