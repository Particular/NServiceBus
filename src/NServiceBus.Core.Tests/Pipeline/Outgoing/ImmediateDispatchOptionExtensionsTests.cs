﻿namespace NServiceBus.Core.Tests.Pipeline.Outgoing;

using NUnit.Framework;

[TestFixture]
public class ImmediateDispatchOptionExtensionsTests
{
    [Test]
    public void RequiresImmediateDispatch_Should_Return_False_When_No_Immediate_Dispatch_Requested()
    {
        var options = new SendOptions();

        Assert.That(options.IsImmediateDispatchSet(), Is.False);
    }

    [Test]
    public void RequiresImmediateDispatch_Should_Return_True_When_Immediate_Dispatch_Requested()
    {
        var options = new SendOptions();
        options.RequireImmediateDispatch();

        Assert.That(options.IsImmediateDispatchSet(), Is.True);
    }
}