namespace NServiceBus.Features
{
    using System.Collections.Generic;

    /// <summary>
    ///     Provides diagnostics data about <see cref="Feature" />s.
    /// </summary>
    public class FeaturesReport
    {
        internal FeaturesReport(IReadOnlyList<FeatureDiagnosticData> data)
        {
            Features = data;
        }

        /// <summary>
        ///     List of <see cref="Feature" />s diagnostic data.
        /// </summary>
        public IReadOnlyList<FeatureDiagnosticData> Features { get; private set; }
    }
}