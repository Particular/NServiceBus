namespace NServiceBus.Core.Tests
{
    using System;
    using NServiceBus.Pipeline.Contexts;

    static class HeaderAssertHelpers
    {
        public static void AssertHeaderWasSet(this PhysicalOutgoingContextStageBehavior.Context context, string key, Predicate<string> predicate)
        {
            Assert(context.GetOrCreate<DispatchMessageToTransportConnector.State>(), key, predicate);
        }

        public static void AssertHeaderWasSet(this OutgoingContext context, string key, Predicate<string> predicate)
        {
            Assert(context.GetOrCreate<DispatchMessageToTransportConnector.State>(), key, predicate);
        }

        static void Assert(DispatchMessageToTransportConnector.State state, string key, Predicate<string> predicate)
        {
            string current;

            if (state.Headers.TryGetValue(key, out current))
            {
                NUnit.Framework.Assert.True(predicate(current), "Header {0} didn't have the expected value", key);
                return;
            }

            NUnit.Framework.Assert.Fail("Header '{0}' was not set", key);
        }
    }
}