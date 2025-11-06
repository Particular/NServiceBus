// This file contains the Enabled property that uses the obsolete Feature.EnableByDefault() method.
// EnableByDefault is obsolete and will be treated as an error in NServiceBus 11.0.
// This file should be deleted when NServiceBus 11.0 is released.
#pragma warning disable CS0618 // Type or member is obsolete

namespace NServiceBus.Core.Tests.Features;

public abstract partial class TestFeature
{
    public bool IsEnabled
    {
        get => IsEnabledByDefault;
        set
        {
            if (value)
            {
                EnableByDefault();
            }
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
