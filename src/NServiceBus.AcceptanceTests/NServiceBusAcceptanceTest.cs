namespace NServiceBus.AcceptanceTests
{
    using System.Linq;
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
            Conventions.EndpointNamingConvention = t =>
            {
                var testName = t.FullName.Split('.').Last();
              
                testName = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(testName);
              
                testName = testName.Replace("+", "");
                testName = testName.Replace("_", "");



                return testName;
            };

            Conventions.DefaultRunDescriptor = () => ScenarioDescriptors.Transports.Default;
        }
    }
}