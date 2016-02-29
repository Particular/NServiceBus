namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Configures the supported scenarios for a test execution
    /// </summary>
    public interface IConfigureSupportedScenariosForTestExecution
    {
        /// <summary>
        /// Scenario descriptors not supported for this test execution
        /// </summary>
        IEnumerable<Type> UnsupportedScenarioDescriptorTypes { get; }
    }
}