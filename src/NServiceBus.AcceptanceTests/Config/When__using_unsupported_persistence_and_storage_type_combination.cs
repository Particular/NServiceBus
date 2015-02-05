namespace NServiceBus.AcceptanceTests.Config
{
    using System.Linq;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Persistence;
    using NServiceBus.Persistence.Legacy;
    using NUnit.Framework;

    public class When__using_unsupported_persistence_and_storage_type_combination : NServiceBusAcceptanceTest
	{
		[Test]
		public void Startup_should_fail()
		{
			var context = Scenario.Define<Context>()
					.WithEndpoint<FailingEndpoint>()
                    .AllowExceptions(e => e.Message.Contains("does not support storage type Timeouts."))
                    .Done(c => c.GetAllLogs().Any(l => l.Level == "error"))
                    .Run();
		}

		public class Context : ScenarioContext
		{
		}

		public class FailingEndpoint : EndpointConfigurationBuilder
		{
			public FailingEndpoint()
			{
				EndpointSetup<DefaultServer>( cfg =>
				{
					cfg.UsePersistence<MsmqPersistence, StorageType.Timeouts>(); //MSMQ only supported for Subscriptions
				} );
			}
		}
	}


}