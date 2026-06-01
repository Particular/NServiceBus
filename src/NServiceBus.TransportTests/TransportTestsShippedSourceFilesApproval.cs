namespace NServiceBus.TransportTests;

using NUnit.Framework;
using Particular.Approvals;

// This test project is shipped as a source package (NServiceBus.TransportTests.Sources) to
// downstream transport implementers. By default, Particular.Packaging includes ALL .cs files
// in the package unless explicitly excluded via RemoveSourceFileFromPackage.
//
// This approval test ensures that adding a new .cs file is a deliberate choice:
// - If the file should be shipped to downstream, the approved list must be updated.
// - If the file should NOT be shipped (e.g. it tests learning-transport internals),
//   it must be added to RemoveSourceFileFromPackage in the .csproj.
//
// IMPORTANT: This test itself is excluded from shipping via RemoveSourceFileFromPackage.
// However, when shipping a new source package release, be aware that changes to the approved
// list will also propagate to downstream repositories. Any newly approved file that is not
// excluded may cause downstream test failures until those repositories are updated.
// Consider validating against a select set of downstream transport repositories before shipping.

[TestFixture]
public class TransportTestsShippedSourceFilesApproval
{
    [Test]
    public void ApproveShippedSourceFiles() => Approver.Verify(ShippedSourceFilesApproval.GetShippedFiles());
}