namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public class UnicastBusAuthorizationTests
    {

        [Test]
        public void Should_use_noop_for_no_authorizer()
        {
            var authorizationType = Features.UnicastBus.FindAuthorizationType(new List<Type>());
            Assert.AreEqual("NoopSubscriptionAuthorizer", authorizationType.Name);
        }

        [Test]
        public void Should_use_single_for_one_authorizer()
        {
            var authorizationType = Features.UnicastBus.FindAuthorizationType(new List<Type>()
            {
                typeof(Authorizer1)
            });
            Assert.AreEqual(typeof(Authorizer1), authorizationType);
        }

        [Test]
        public void Should_throw_for_multiple_authorizer()
        {
            var exception = Assert.Throws<Exception>(() => Features.UnicastBus.FindAuthorizationType(new List<Type>()
            {
                typeof(Authorizer1),
                typeof(Authorizer2)
            }));
            Assert.AreEqual("Only one instance of IAuthorizeSubscriptions is allowed. Found the following: 'NServiceBus.Unicast.Tests.UnicastBusAuthorizationTests+Authorizer1', 'NServiceBus.Unicast.Tests.UnicastBusAuthorizationTests+Authorizer2'.", exception.Message);
        }

        class Authorizer1 : IAuthorizeSubscriptions
        {
            public bool AuthorizeSubscribe(string messageType, string clientEndpoint, IDictionary<string, string> headers)
            {
                throw new NotImplementedException();
            }

            public bool AuthorizeUnsubscribe(string messageType, string clientEndpoint, IDictionary<string, string> headers)
            {
                throw new NotImplementedException();
            }
        }

        class Authorizer2 : IAuthorizeSubscriptions
        {
            public bool AuthorizeSubscribe(string messageType, string clientEndpoint, IDictionary<string, string> headers)
            {
                throw new NotImplementedException();
            }

            public bool AuthorizeUnsubscribe(string messageType, string clientEndpoint, IDictionary<string, string> headers)
            {
                throw new NotImplementedException();
            }
        }
    }
}