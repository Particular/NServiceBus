namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Linq;
    using AcceptanceTesting.Customization;
    using NUnit.Framework;

    /// <summary>
    /// Base class for all the NSB test that sets up our conventions
    /// </summary>
    [TestFixture]
    public abstract class NServiceBusAcceptanceTest
    {
        [SetUp]
        public void SetUp()
        {
            Conventions.EndpointNamingConvention = t =>
            {
                var classAndEndpoint = t.FullName.Split('.').Last();

                var testName = classAndEndpoint.Split('+').First();

                testName = testName.Replace("When_", "");

                var endpointBuilder = classAndEndpoint.Split('+').Last();

                testName = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(testName);
                testName = testName.Replace("_", "");

                return testName +"."+ endpointBuilder;
            };
        }

        [TearDown]
        public void TearDown()
        {
            File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "instance-mapping.xml"));
        }
    }
}