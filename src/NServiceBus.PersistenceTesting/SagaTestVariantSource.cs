namespace NServiceBus.PersistenceTesting
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    // ReSharper disable once PartialTypeWithSinglePart
    public partial class SagaTestVariantSource
    {
        public static IEnumerable Variants
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