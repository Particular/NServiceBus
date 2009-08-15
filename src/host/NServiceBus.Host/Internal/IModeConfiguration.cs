using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NServiceBus.Host.Internal
{
    public interface IModeConfiguration
    {
        void Init(IConfigureThisEndpoint specifier);
        void ConfigureLogging();
        void ConfigureSagas(Configure busConfiguration);
        void ConfigureSubscriptionStorage(Configure busConfiguration);
    }
}
