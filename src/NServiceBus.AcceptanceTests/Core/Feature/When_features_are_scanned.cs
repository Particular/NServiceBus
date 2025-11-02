namespace NServiceBus.AcceptanceTests.Core.Feature;

using System.Threading.Tasks;
using AcceptanceTesting;
using NUnit.Framework;

public partial class When_features_are_scanned : NServiceBusAcceptanceTest
{
    class Context : ScenarioContext
    {
        public bool RootFeatureCalled { get; set; }
        public bool DependentFeatureCalled { get; set; }
    }
}