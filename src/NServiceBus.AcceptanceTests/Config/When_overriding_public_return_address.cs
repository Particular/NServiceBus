//namespace NServiceBus.AcceptanceTests.Config
//{
//    using System.Reflection;
//    using NServiceBus.AcceptanceTesting;
//    using NServiceBus.AcceptanceTests.EndpointTemplates;
//    using NServiceBus.Configuration.AdvanceExtensibility;
//    using NServiceBus.Settings;
//    using NUnit.Framework;

//    public class When_overriding_public_return_address : NServiceBusAcceptanceTest
//    {
//        [Test]
//        public void Should_allow_endpoint_name_to_be_used()
//        {
//            var context = Scenario.Define<Context>()
//                    .WithEndpoint<EndpointWithCustomAddress>(b => b.CustomConfig(c => c.GetSettings().Set("UseEndpointNameAsPublicReturnAddress",true)))
//                    .Done(c => c.IsDone)
//                    .Run();

//            Assert.AreEqual(context.ReplyToAddress,context.EndpointName);
//        }

//        [Test]
//        public void Should_use_explicit_address_if_found()
//        {
//            var context = Scenario.Define<Context>()
//                    .WithEndpoint<EndpointWithCustomAddress>(b =>
//                    {
//                        b.CustomConfig(c => c.OverridePublicReturnAddress("Explicit"));
//                        b.CustomConfig(c => c.GetSettings().Set("UseEndpointNameAsPublicReturnAddress", true));
//                    })
//                    .Done(c => c.IsDone)
//                    .Run();

//            Assert.AreEqual(context.ReplyToAddress, "Explicit");
//        }

//        public class Context : ScenarioContext
//        {
//            public bool IsDone { get; set; }
//            public string ReplyToAddress { get; set; }
//            public string EndpointName { get; set; }
//        }

//        public class EndpointWithCustomAddress : EndpointConfigurationBuilder
//        {
//            public EndpointWithCustomAddress()
//            {
//                EndpointSetup<DefaultServer>();
//            }

//            class AfterConfigIsComplete:IWantToRunWhenBusStartsAndStops
//            {
//                public Context Context { get; set; }

//                public ReadOnlySettings Settings { get; set; }

//                public Configure Config { get; set; }

//                public void Start()
//                {
//                    var propInfo = Config.GetType().GetProperty("PublicReturnAddress", BindingFlags.Instance | BindingFlags.NonPublic);

//                    Context.ReplyToAddress = (string)propInfo.GetValue(Config, null);


//                    Context.EndpointName = Settings.EndpointName();

//                    Context.IsDone = true;
//                }

//                public void Stop()
//                {
//                }
//            }
//        }
//    }


//}