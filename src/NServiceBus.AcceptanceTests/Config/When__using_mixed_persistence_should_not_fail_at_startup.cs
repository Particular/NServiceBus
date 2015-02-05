namespace NServiceBus.AcceptanceTests.Config
{
	using NServiceBus.AcceptanceTesting;
	using NServiceBus.AcceptanceTests.EndpointTemplates;
	using NServiceBus.Persistence;
	using NServiceBus.Persistence.Legacy;
	using NServiceBus.Settings;
	using NUnit.Framework;

	public class When__using_mixed_persistence_should_not_fail_at_startup : NServiceBusAcceptanceTest
	{
		[Test]
		public void Configure_and_setting_should_be_available_via_DI()
		{
			var context = Scenario.Define<Context>()
					.WithEndpoint<StartedEndpoint>()
					.Done( c => c.IsDone )
					.Run();
		}

		public class Context : ScenarioContext
		{
			public bool IsDone { get; set; }
		}

		public class StartedEndpoint : EndpointConfigurationBuilder
		{
			public StartedEndpoint()
			{
				EndpointSetup<DefaultServer>( cfg =>
				{
					cfg.UsePersistence<MsmqPersistence, StorageType.Timeouts>();
					cfg.UsePersistence<InMemoryPersistence, StorageType.GatewayDeduplication>();
					cfg.UsePersistence<InMemoryPersistence, StorageType.Sagas>();
					cfg.UsePersistence<InMemoryPersistence, StorageType.Outbox>();
					cfg.UsePersistence<InMemoryPersistence, StorageType.Subscriptions>();
				} );
			}

			class AfterConfigIsComplete : IWantToRunWhenBusStartsAndStops
			{
				public Context Context { get; set; }

				public Configure Configure { get; set; }

				public ReadOnlySettings Settings { get; set; }


				public void Start()
				{
					Context.IsDone = true;
				}

				public void Stop()
				{
				}
			}
		}
	}


}