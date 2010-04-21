using System;
using System.Collections.Generic;

namespace ObjectBuilder.Tests
{
    public class ClassWithSetterDependency
    {
        public ISomeDependency Dependency { get; set; }
        public IList<string> SystemDependency { get; set; }
        public SomeEnum EnumDependency { get; set; }
        public int SimpleDependecy { get; set; }
        public string StringDependecy { get; set; }

    }

    public enum SomeEnum
    {
        X
    }

    public interface ISomeDependency { }

    public class ClassThatImplementsDependency : ISomeDependency
    {
        private readonly int hashCode = new Random().Next();
        
        public override int GetHashCode()
        {
            return this.hashCode;
        }
    }

    public interface INonConfiguredInterface { }
}