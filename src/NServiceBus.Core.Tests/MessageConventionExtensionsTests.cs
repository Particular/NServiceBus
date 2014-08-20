namespace NServiceBus.Core.Tests
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using Unicast.Messages;
    using NUnit.Framework;

    [TestFixture]
    public class MessageConventionExtensionsTests
    {
        [Test]
        public void Should_use_TimeToBeReceived_from_bottom_of_tree()
        {
            var timeToBeReceivedAction = MessageConventionExtensions.TimeToBeReceivedAction(typeof(InheritedClassWithAttribute));
            Assert.AreEqual(TimeSpan.FromSeconds(2), timeToBeReceivedAction);
        }

        [Test]
        public void Should_use_inherited_TimeToBeReceived()
        {
            var timeToBeReceivedAction = MessageConventionExtensions.TimeToBeReceivedAction(typeof(InheritedClassWithNoAttribute));
            Assert.AreEqual(TimeSpan.FromSeconds(1), timeToBeReceivedAction);
        }

        [TimeToBeReceivedAttribute("00:00:01")]
        class BaseClass
        {
        }
        [TimeToBeReceivedAttribute("00:00:02")]
        class InheritedClassWithAttribute : BaseClass
        {
            
        }
        class InheritedClassWithNoAttribute : BaseClass
        {
            
        }


        [Test]
        public void Should_return_false_for_SN_and_non_particular_assembly()
        {
            Assert.IsFalse(MessageConventionExtensions.IsFromParticularAssembly(typeof(string)));
        }

        [Test]
        public void Should_return_true_for_particular_assembly()
        {
            Assert.IsTrue(MessageConventionExtensions.IsFromParticularAssembly(typeof(ExecuteLogicalMessagesBehavior)));
        }

        [Test]
        public void Should_return_false_for_non_SN_and_non_particular_assembly()
        {
            var type = GetNonSnFakeType();
            Assert.IsFalse(MessageConventionExtensions.IsFromParticularAssembly(type));
        }

        static Type GetNonSnFakeType()
        {
            var assemblyName = new AssemblyName
            {
                Name = "myAssembly"
            };
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.ReflectionOnly);
            var newModule = assemblyBuilder.DefineDynamicModule("myModule");
            var myType = newModule.DefineType("myType", TypeAttributes.Public);
            return myType.CreateType();
        }
    }

}