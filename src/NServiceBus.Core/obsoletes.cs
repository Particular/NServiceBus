// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedParameter.Global

#pragma warning disable 1591

namespace NServiceBus
{
}


namespace NServiceBus.Features
{
    // Just to make sure we remove it in v8. We keep it around for now just in case some external feature
    // depended on it using `DependsOn(string featureTypeName)` and also to set the host id default, see below.
    [ObsoleteEx(
           RemoveInVersion = "8",
           TreatAsErrorFromVersion = "7")]
    class HostInformationFeature : Feature
    {
        public HostInformationFeature()
        {
            EnableByDefault();

            // To allow users to avoid MD5 to be used by adding a hostid in a Feature default this have to stay here to maintain comaptibility.
            //For more details see the test: When_feature_overrides_hostid_from_feature_default
            Defaults(settings => settings.Get<HostingComponent.Settings>().ApplyHostIdDefaultIfNeededForV7BackwardsCompatibility());
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
        }
    }
}

#pragma warning restore 1591