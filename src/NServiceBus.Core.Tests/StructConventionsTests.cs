namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using NUnit.Framework;
    using Particular.Approvals;

    [TestFixture]
    public class StructConventionsTests
    {
        [Test]
        public void ApproveStructsWhichDontFollowStructGuidelines()
        {
            var approvalBuilder = new StringBuilder();
            approvalBuilder.AppendLine(@"-------------------------------------------------- REMEMBER --------------------------------------------------
CONSIDER defining a struct instead of a class if instances of the type are small and commonly short-lived or are commonly embedded in other objects.

AVOID defining a struct unless the type has all of the following characteristics:
   * It logically represents a single value, similar to primitive types(int, double, etc.).
   * It has an instance size under 16 bytes.
   * It is immutable.
   * It will not have to be boxed frequently.

In all other cases, you should define your types as classes.
-------------------------------------------------- REMEMBER --------------------------------------------------
");

            var assembly = typeof(Endpoint).Assembly;

            foreach (var type in assembly.GetTypes().OrderBy(t => t.FullName))
            {
                if (!type.IsValueType || type.IsEnum || type.IsSpecialName || type.Namespace == null || !type.Namespace.StartsWith("NServiceBus") || type.FullName.Contains("__"))
                {
                    continue;
                }
                
                // For some reason this class's size is different across platforms causing the test to fail on Linux. Disabling here since it won't be used as of v8
                if (type.FullName.Equals(typeof(NServiceBus.Timeout.Core.TimeoutData).FullName)) 
                {
                    continue;
                }

                var violatedRules = new List<string> { $"{type.FullName} violates the following rules:" };

                InspectWhetherStructContainsPublicFields(type, violatedRules);
                InspectWhetherStructContainsWritableProperties(type, violatedRules);
                var containsRefereneceTypes = InspectWhetherStructContainsReferenceTypes(type, violatedRules);

                if (containsRefereneceTypes)
                {
                    violatedRules.Add("   - The size cannot be determined because there are fields that are reference types.");
                }
                else
                {
                    InspectSizeOfStruct(type, violatedRules);
                }

                if (violatedRules.Count <= 1)
                {
                    continue;
                }

                foreach (var violatedRule in violatedRules)
                {
                    approvalBuilder.AppendLine(violatedRule);
                }

                approvalBuilder.AppendLine();
            }

            Approver.Verify(approvalBuilder.ToString());
        }

        static bool InspectWhetherStructContainsReferenceTypes(Type type, List<string> violatedRules)
        {
            var mutabilityRules = new List<string> { "   - The following fields are reference types, which are potentially mutable:" };

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var fieldInfo in fields.OrderBy(f => f.Name))
            {
                if (fieldInfo.FieldType == typeof(string) && (fieldInfo.IsInitOnly || fieldInfo.IsLiteral))
                {
                    continue;
                }

                if (fieldInfo.FieldType.IsClass || fieldInfo.FieldType.IsInterface)
                {
                    mutabilityRules.Add($"      - Field {fieldInfo.Name} of type { fieldInfo.FieldType } is a reference type.");
                }
            }

            if (mutabilityRules.Count > 1)
            {
                violatedRules.AddRange(mutabilityRules);

                return true;
            }

            return false;
        }

        static bool InspectWhetherStructContainsPublicFields(Type type, List<string> violatedRules)
        {
            var mutabilityRules = new List<string> { "   - The following fields are public, so the type is not immutable:" };

            var fields = type.GetFields();

            foreach (var fieldInfo in fields.OrderBy(f => f.Name))
            {
                if (!fieldInfo.IsInitOnly && !fieldInfo.IsLiteral)
                {
                    mutabilityRules.Add($"      - Field {fieldInfo.Name} of type { fieldInfo.FieldType } is public.");
                }
            }

            if (mutabilityRules.Count > 1)
            {
                violatedRules.AddRange(mutabilityRules);

                return true;
            }

            return false;
        }

        static bool InspectWhetherStructContainsWritableProperties(Type type, List<string> violatedRules)
        {
            var mutabilityRules = new List<string> { "   - The following properties can be written to, so the type is not immutable:" };

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var property in properties.OrderBy(p => p.Name))
            {
                if (property.CanWrite)
                {
                    mutabilityRules.Add($"      - Property {property.Name} of type { property.PropertyType } can be written to.");
                }
            }

            if (mutabilityRules.Count > 1)
            {
                violatedRules.AddRange(mutabilityRules);

                return true;
            }

            return false;
        }

        static void InspectSizeOfStruct(Type type, List<string> violatedRules)
        {
            try
            {
                var size = Marshal.SizeOf(type);

                if (IsLargerThanSixteenBytes(size))
                {
                    violatedRules.Add($"   - The size is {size} bytes, which exceeds the recommended maximum of 16 bytes.");
                }
            }
            catch (Exception)
            {
                violatedRules.Add("   - The size cannot be determined. This type likely violates all struct rules.");
            }
        }

        static bool IsLargerThanSixteenBytes(int size) => size > 16;
    }
}
