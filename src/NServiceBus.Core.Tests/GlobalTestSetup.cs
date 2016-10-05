namespace NServiceBus.Core.Tests
{
    using System.Globalization;
    using System.Threading;
    using NUnit.Framework;

    [SetUpFixture]
    public class GlobalTestSetup
    {
        [OneTimeSetUp]
        public void Initialize()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        }
    }
}