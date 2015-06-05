namespace NServiceBus.AcceptanceTesting.Customization
{
    using System;
    using System.Collections.Generic;

    public static class BusConfigExtensions
    {
        /// <summary>
        /// Backdoor into the core types to scan. This allows people to test a subset of functionality when running Acceptance tests
        /// </summary>
        /// <param name="config">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        /// <param name="typesToScan">Override the types to scan.</param>
        public static void TypesToIncludeInScan(this BusConfiguration config, IEnumerable<Type> typesToScan)
        {
            config.TypesToScanInternal(typesToScan);
        }
    }
}