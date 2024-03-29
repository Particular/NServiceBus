﻿namespace NServiceBus.AcceptanceTests;

using NUnit.Framework;

static partial class Requires
{
    public static void DtcSupport()
    {
        if (!TestSuiteConstraints.Current.SupportsDtc)
        {
            Assert.Ignore("Ignoring this test because it requires DTC transaction support from the transport.");
        }
    }

    public static void CrossQueueTransactionSupport()
    {
        if (!TestSuiteConstraints.Current.SupportsCrossQueueTransactions)
        {
            Assert.Ignore("Ignoring this test because it requires cross queue transaction support from the transport.");
        }
    }

    public static void NativePubSubSupport()
    {
        if (!TestSuiteConstraints.Current.SupportsNativePubSub)
        {
            Assert.Ignore("Ignoring this test because it requires native publish subscribe support from the transport.");
        }
    }

    public static void MessageDrivenPubSub()
    {
        if (TestSuiteConstraints.Current.SupportsNativePubSub)
        {
            Assert.Ignore("Ignoring this test because it requires message driven publish subscribe but this test suite uses native publish subscribe.");
        }
    }

    public static void DelayedDelivery()
    {
        if (!TestSuiteConstraints.Current.SupportsDelayedDelivery)
        {
            Assert.Ignore("Ignoring this test because it requires delayed delivery support from the transport.");
        }
    }

    public static void OutboxPersistence()
    {
        if (!TestSuiteConstraints.Current.SupportsOutbox)
        {
            Assert.Ignore("Ignoring this tests because it requires a persistence providing an Outbox storage.");
        }
    }

    public static void PurgeOnStartupSupport()
    {
        if (!TestSuiteConstraints.Current.SupportsPurgeOnStartup)
        {
            Assert.Ignore("Ignoring this tests because it requires a transport able to purge queues at startup.");
        }
    }
}