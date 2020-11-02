using System.Linq;

namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;

    public class FakeTransportInfrastructure : TransportInfrastructure
    {
        public FakeTransportInfrastructure(Settings settings, FakeTransport fakeTransportSettings,
            ReceiveSettings[] receiveSettingses)
        {
            Dispatcher = new FakeDispatcher();
            Receivers = receiveSettingses
                .Select(s => new FakeReceiver(fakeTransportSettings, settings.CriticalErrorAction, s.Id)).ToArray();
        }

        public override ValueTask DisposeAsync()
        {
            return new ValueTask();
        }
    }
}