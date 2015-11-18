namespace NServiceBus.Core.Utils.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using NUnit.Framework;

    [TestFixture]
    public class ExtensionMethodsTests
    {
        [Test]
        public void SerializationFriendlyNameTests()
        {
            Assert.AreEqual("String", typeof(string).SerializationFriendlyName());
            Assert.AreEqual("DictionaryOfStringAndInt32",typeof(Dictionary<string, int>).SerializationFriendlyName());
            Assert.AreEqual("DictionaryOfStringAndTupleOfInt32", typeof(Dictionary<string, Tuple<int>>).SerializationFriendlyName());
            Assert.AreEqual("NServiceBus.KeyValuePairOfStringAndTupleOfInt32", typeof(KeyValuePair<string, Tuple<int>>).SerializationFriendlyName());
        }

        [Test]
        public void Should_return_return_different_results_for_different_types()
        {
            // This test verifies whether the added cache doesn't break the execution if called successively for two different types

            var customTypeResult = typeof(Target).IsSystemType();
            var systemTypeResult = typeof(string).IsSystemType();

            Assert.IsTrue(systemTypeResult, "Expected string to be a system type.");
            Assert.IsFalse(customTypeResult, "Expected Target to be a custom type.");
        }

        public class Target
        {
            public string Property1 { get; set; }
        }

        [Test]
        public void Should_return_false_for_SN_and_non_particular_assembly()
        {
            Assert.IsFalse(typeof(string).IsFromParticularAssembly());
        }

        [Test]
        public void Should_return_true_for_particular_assembly()
        {
            Assert.IsTrue(typeof(TransportReceiveToPhysicalMessageProcessingConnector).IsFromParticularAssembly());
        }

        [Test]
        public void Should_return_false_for_non_SN_and_non_particular_assembly()
        {
            var type = GetNonSnFakeType();
            Assert.IsFalse(type.IsFromParticularAssembly());
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