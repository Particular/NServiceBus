namespace NServiceBus.Core.Tests.Features
{
    using System.Collections.Generic;
    using NServiceBus.Features;
    using NUnit.Framework;

    [TestFixture]
    public class FeatureCategories
    {
         [Test]
         public void Equality()
         {
             Assert.AreEqual(new MyCategory(),new MyCategory());



             Assert.True(new MyCategory() == new MyCategory());
             Assert.AreNotEqual(new AnotherCategory(), new MyCategory());

             var collection = new List<FeatureCategory>{new MyCategory(), new AnotherCategory(), FeatureCategory.None};


             Assert.Contains(new MyCategory(), collection);
         }

        class MyCategory:FeatureCategory
        {
            
 
        }

        class AnotherCategory : FeatureCategory
        {

        }
    }
}