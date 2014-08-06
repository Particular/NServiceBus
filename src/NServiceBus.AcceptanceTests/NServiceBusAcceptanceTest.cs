namespace NServiceBus.AcceptanceTests
{
    using System.Threading;
    using AcceptanceTesting.Customization;
    using NUnit.Framework;

    /// <summary>
    /// Base class for all the NSB test that sets up our conventions
    /// </summary>
    [TestFixture]
// ReSharper disable once PartialTypeWithSinglePart
    public abstract partial class NServiceBusAcceptanceTest
    {
        [SetUp]
        public void SetUp()
        {
            Conventions.EndpointNamingConvention= t =>
                {
                    var baseNs = typeof (NServiceBusAcceptanceTest).Namespace;
                    var testName = GetType().Name;
                    return t.FullName.Replace(baseNs + ".", "").Replace(testName + "+", "")
                            + "." + Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(testName).Replace("_", "");
                };

            Conventions.DefaultRunDescriptor = () => ScenarioDescriptors.Transports.Default;
        }
    }
}