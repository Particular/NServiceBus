namespace NServiceBus.PersistenceTesting
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    // ReSharper disable once PartialTypeWithSinglePart
    public partial class OutboxTestVariantSource
    {
        public static IEnumerable<TestFixtureData> Variants
        {
            get
            {
                var caseList = new List<TestVariant>();
                GenerateCases(caseList);
                return caseList.Select(x => new TestFixtureData(x));
            }
        }

        static partial void GenerateCases(List<TestVariant> caseList);
    }
}