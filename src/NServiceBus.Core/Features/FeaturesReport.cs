namespace NServiceBus.Features
{
    using System.Collections.Generic;

    class FeaturesReport
    {
        internal FeaturesReport(IReadOnlyList<FeatureDiagnosticData> data)
        {
            Features = data;
        }

        public IReadOnlyList<FeatureDiagnosticData> Features { get; private set; }
    }
}