namespace NServiceBus.Core.Tests.Diagnostics
{
    using NUnit.Framework;
    using Particular.Approvals;

    [TestFixture]
    public class JsonPrettifierTests
    {
        [TestCase]
        public void Print_ShouldPrettifyJsonContent()
        {
            var json = @"{""Container"":{""Type"":""NServiceBus.CommonObjectBuilder""}}";
            var prettified = JsonPrettyPrinter.Print(json);
            Approver.Verify(prettified);
        }
        
        [TestCase]
        public void Print_ShouldPrettifyCollections()
        {
            var json = @"{""Dependencies"":[""RootFeature"",""DelayedDeliveryFeature""],""StartupTasks"":[]}";
            var prettified = JsonPrettyPrinter.Print(json);
            Approver.Verify(prettified);
        }
        
        [TestCase]
        public void Print_ShouldPrettifyEscapedStrings()
        {
            var json = @"{""PrerequisiteStatus"": {""IsSatisfied"": false, ""Reasons"": [""Because some \""escaped\"" json content""]}}";
            var prettified = JsonPrettyPrinter.Print(json);
            Approver.Verify(prettified);
        }
    }
}