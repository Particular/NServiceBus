namespace NServiceBus.Core.Tests
{
    using System;
    using System.Globalization;
    using System.Threading;
    using NUnit.Framework;
    using NUnit.Framework.Interfaces;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    class TestWithInvariantCultureAttribute : Attribute, ITestAction
    {
        public void BeforeTest(ITest test)
        {
            currentCulture = Thread.CurrentThread.CurrentUICulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        }

        public void AfterTest(ITest test)
        {
            Thread.CurrentThread.CurrentUICulture = currentCulture;
        }

        public ActionTargets Targets { get; } = ActionTargets.Default;

        CultureInfo currentCulture;
    }
}