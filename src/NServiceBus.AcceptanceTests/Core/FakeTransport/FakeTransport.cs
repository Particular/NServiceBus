﻿namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Collections.Generic;
    using Settings;
    using Transport;

    public class FakeTransport : TransportDefinition
    {
        public override bool RequiresConnectionString => false;

        public override string ExampleConnectionStringForErrorMessage => null;

        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            settings.GetOrCreate<StartUpSequence>().Add($"{nameof(TransportDefinition)}.{nameof(Initialize)}");

            if (settings.TryGet<Action<ReadOnlySettings>>("FakeTransport.AssertSettings", out var assertion))
            {
                assertion(settings);
            }

            return new FakeTransportInfrastructure(settings);
        }

        public class StartUpSequence : List<string> { }
    }
}