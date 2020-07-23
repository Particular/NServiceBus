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

                if (caseList.Count == 0) //No defined variants
                {
                    caseList.Add(new TestVariant()); //Use single default variant
                }

                return caseList.Select(x => new TestFixtureData(x));
            }
        }

        static partial void GenerateCases(List<TestVariant> caseList);
    }
}