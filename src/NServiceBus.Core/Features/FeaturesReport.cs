namespace NServiceBus.Features
{
    using System.Collections.Generic;

    class FeaturesReport : List<FeatureDiagnosticData>
    {
        public FeaturesReport(IEnumerable<FeatureDiagnosticData> collection) : base(collection)
        {
        }
    }
}