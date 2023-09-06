namespace NServiceBus.AcceptanceTesting.Support
{
    using System;

    public interface ITestSuiteConstraints
    {
        bool SupportsDtc { get; }

        bool SupportsCrossQueueTransactions { get; }

        bool SupportsNativePubSub { get; }

        bool SupportsDelayedDelivery { get; }

        bool SupportsOutbox { get; }

        bool SupportsPurgeOnStartup { get; }

        IConfigureEndpointTestExecution CreateTransportConfiguration();

        IConfigureEndpointTestExecution CreatePersistenceConfiguration();

        static ITestSuiteConstraints current;
        static ITestSuiteConstraints Current
        {
            get
            {
                if (current == null)
                {
                    throw new NotImplementedException("Tests setup is not completed. Create a module initializer by adding a class with a public static method decorated with the ModuleInitializer .NET attribute." +
                                                      " In the static method, create an instance of the ITestSuiteConstraints implementation in your test project and sets it to the ITestSuiteConstraints.Current property.");
                }

                return current;
            }
            set => current = value;
        }
    }
}